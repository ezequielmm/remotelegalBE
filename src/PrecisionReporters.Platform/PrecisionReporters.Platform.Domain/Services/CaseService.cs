using FluentResults;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class CaseService : ICaseService
    {
        private readonly ICaseRepository _caseRepository;
        private readonly IUserService _userService;
        private readonly IDocumentService _documentService;
        private readonly IDepositionService _depositionService;
        private readonly ILogger<CaseService> _logger;
        private readonly ITransactionHandler _transactionHandler;
        private readonly IPermissionService _permissionService;

        public CaseService(ICaseRepository caseRepository, IUserService userService, IDocumentService documentService, IDepositionService depositionService, ILogger<CaseService> logger, ITransactionHandler transactionHandler, IPermissionService permissionService)
        {
            _caseRepository = caseRepository;
            _userService = userService;
            _logger = logger;
            _documentService = documentService;
            _depositionService = depositionService;
            _transactionHandler = transactionHandler;
            _permissionService = permissionService;
        }

        public async Task<List<Case>> GetCases(Expression<Func<Case, bool>> filter = null, string[] include = null)
        {
            return await _caseRepository.GetByFilter(filter, include);
        }

        public async Task<Result<Case>> GetCaseById(Guid id, string[] include = null)
        {
            var foundCase = await _caseRepository.GetById(id, include);
            if (foundCase == null)
                return Result.Fail(new ResourceNotFoundError($"Case with id {id} not found."));

            return Result.Ok(foundCase);
        }

        public async Task<Result<Case>> CreateCase(string userEmail, Case newCase)
        {
            var userResult = await _userService.GetUserByEmail(userEmail);
            if (userResult.IsFailed)
            {
                return userResult.ToResult<Case>();
            }

            var user = userResult.Value;
            Result<Case> caseCreationResult = null;
            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                newCase.AddedBy = user;
                newCase.Members = new List<Member> { new Member { User = user } };

                var createdCase = await _caseRepository.Create(newCase);

                var roleCreationResult = await _permissionService.AddUserRole(user.Id, createdCase.Id, ResourceType.Case, RoleName.CaseAdmin);
                caseCreationResult = !roleCreationResult.IsFailed ? Result.Ok(createdCase) : Result.Fail<Case>(new UnexpectedError("There was an error trying to create a case"));
            });

            if (transactionResult.IsFailed)
            {
                return transactionResult;
            }

            return caseCreationResult;
        }

        public async Task<Result<List<Case>>> GetCasesForUser(string userEmail, CaseSortField? sortedField = null, SortDirection? sortDirection = null)
        {
            var userResult = await _userService.GetUserByEmail(userEmail);
            if (userResult.IsFailed)
            {
                return userResult.ToResult<List<Case>>();
            }

            var includes = new[] { nameof(Case.AddedBy), nameof(Case.Members) };

            // TODO: Move this to BaseRepository and find a generic way to apply OrderBy
            Expression<Func<Case, object>> orderBy = sortedField switch
            {
                CaseSortField.AddedBy => x => x.AddedBy.FirstName,
                CaseSortField.CaseNumber => x => x.CaseNumber,
                CaseSortField.CreatedDate => x => x.CreationDate,
                _ => x => x.Name,
            };

            Expression<Func<Case, object>> orderByThen = x => x.AddedBy.LastName;
            
            var foundCases = await _caseRepository.GetByFilterOrderByThen(
                orderBy,
                sortDirection ?? SortDirection.Ascend,
                x => x.Members.Any(m => m.UserId == userResult.Value.Id),
                includes,
                sortedField == CaseSortField.AddedBy ? orderByThen : null);

            // note: empty list is ok, null is an error
            if (foundCases == null)
                return Result.Fail(new ResourceNotFoundError()); // TODO: What would cause foundCases to be null? Use the right type of error.

            return Result.Ok(foundCases);
        }

        public async Task<Result<Case>> ScheduleDepositions(string userEmail, Guid caseId, IEnumerable<Deposition> depositions, Dictionary<string, FileTransferInfo> files)
        {
            var userResult = await _userService.GetUserByEmail(userEmail);
            if (userResult.IsFailed)
                return userResult.ToResult<Case>();

            var caseToUpdate = await _caseRepository.GetFirstOrDefaultByFilter(x => x.Id == caseId, new[] { nameof(Case.Depositions), nameof(Case.Members) });
            if (caseToUpdate == null)
                return Result.Fail(new ResourceNotFoundError($"Case with id {caseId} not found."));

            var uploadedDocuments = new List<Document>();

            try
            {
                var transactionResult = await _transactionHandler.RunAsync<Case>(async ()=>
                {
                    // Upload only files related to a caption
                    var filesToUpload = files.Where(f => depositions.Select(d => d.FileKey).ToList().Contains(f.Key));

                    foreach (var file in filesToUpload)
                    {
                        var documentResult = await _documentService.UploadDocumentFile(file, userResult.Value, $"{caseId}/caption");
                        if (documentResult.IsFailed)
                        {
                            _logger.LogError(new Exception(documentResult.Errors.First().Message), "Unable to load one or more documents to storage");
                            _logger.LogInformation("Removing uploaded documents");
                            await _documentService.DeleteUploadedFiles(uploadedDocuments);
                            return Result.Fail(new Error("Unable to upload one or more documents to deposition"));
                        }
                        uploadedDocuments.Add(documentResult.Value);
                    }

                    foreach (var deposition in depositions)
                    {
                        var depositionResult = await _depositionService.GenerateScheduledDeposition(caseToUpdate.Id, deposition, uploadedDocuments, userResult.Value);
                        if (depositionResult.IsFailed)
                        {
                            await _documentService.DeleteUploadedFiles(uploadedDocuments);
                            return depositionResult.ToResult<Case>();
                        }
                        var newDeposition = depositionResult.Value;

                        if (newDeposition.Participants != null)
                        {
                            foreach (var participant in newDeposition.Participants.Where(participant => participant.User != null))
                            {
                                AddMemberToCase(participant.User, caseToUpdate);
                            }
                        }

                        AddMemberToCase(newDeposition.Requester, caseToUpdate);
                    }

                    await _caseRepository.Update(caseToUpdate);

                    return Result.Ok(caseToUpdate);
                });

                return transactionResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to schedule depositions");
                await _documentService.DeleteUploadedFiles(uploadedDocuments);
                return Result.Fail(new ExceptionalError("Unable to schedule depositions", ex));
            }
        }

        private void AddMemberToCase(User userToAdd, Case caseToUpdate)
        {
            if (userToAdd != null && caseToUpdate.Members.All(m => m.UserId != userToAdd.Id))
                caseToUpdate.Members.Add(new Member { User = userToAdd });
        }
    }
}
