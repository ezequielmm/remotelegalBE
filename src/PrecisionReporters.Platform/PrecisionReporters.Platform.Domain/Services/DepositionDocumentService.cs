using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class DepositionDocumentService : IDepositionDocumentService
    {
        private readonly IDepositionDocumentRepository _depositionDocumentRepository;
        private readonly IAnnotationEventService _annotationEventService;
        private readonly IDocumentService _documentService;
        private readonly IDepositionService _depositionService;
        private readonly IUserService _userService;
        private readonly ITransactionHandler _transactionHandler;
        private readonly IDepositionRepository _depositionRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IAwsStorageService _awsStorageService;
        private readonly DocumentConfiguration _documentsConfiguration;
        private readonly ILogger<DepositionDocumentService> _logger;

        public DepositionDocumentService(IDepositionDocumentRepository depositionDocumentRepository,
            IAnnotationEventService annotationEventService,
            IDocumentService documentService,
            IDepositionService depositionService,
            IUserService userService,
            ITransactionHandler transactionHandler,
            IDepositionRepository depositionRepository,
            IDocumentRepository documentRepository,
            IAwsStorageService awsStorageService,
            IOptions<DocumentConfiguration> documentConfigurations, ILogger<DepositionDocumentService> logger)
        {
            _documentsConfiguration = documentConfigurations.Value ?? throw new ArgumentException(nameof(documentConfigurations));
            _depositionDocumentRepository = depositionDocumentRepository;
            _annotationEventService = annotationEventService;
            _documentService = documentService;
            _depositionService = depositionService;
            _userService = userService;
            _transactionHandler = transactionHandler;
            _depositionRepository = depositionRepository;
            _documentRepository = documentRepository;
            _awsStorageService = awsStorageService;
            _logger = logger;
        }

        public async Task<Result> CloseStampedDepositionDocument(Document document, DepositionDocument depositionDocument, string identity, string temporalPath)
        {
            var canCloseDocument = await ParticipantCanCloseDocument(document, depositionDocument.DepositionId);
            if (!canCloseDocument)
                return Result.Fail(new ForbiddenError());

            DepositionDocument newDepositionDocument = null;
            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                newDepositionDocument = await _depositionDocumentRepository.Create(depositionDocument);
                // Update document in S3 and Delete entry from DocumentUserDepositions table
                var uploadResult = await _documentService.UpdateDocument(document, newDepositionDocument, identity, temporalPath);
                if (uploadResult.IsFailed)
                    return uploadResult.ToResult<bool>();
                
                var removeUserDocumentResult = await _documentService.RemoveDepositionUserDocuments(depositionDocument.DocumentId);
                if (removeUserDocumentResult.IsFailed)
                    return removeUserDocumentResult.ToResult<bool>();

                await RemoveAnnotationEvents(depositionDocument.DocumentId);
                await ClearDepositionDocumentSharingId(depositionDocument.DepositionId);

                return Result.Ok(true);
            });

            if (transactionResult.IsFailed)
                return transactionResult;

            return Result.Ok();
        }

        public async Task<Result> CloseDepositionDocument(Document document, Guid depostionId)
        {
            var canCloseDocument = await ParticipantCanCloseDocument(document, depostionId);
            if (!canCloseDocument)
                return Result.Fail(new ForbiddenError());

            var removedAnnotations = await RemoveAnnotationEvents(document.Id);
            var removedSharingId = await ClearDepositionDocumentSharingId(depostionId);

            if (removedAnnotations.IsFailed || removedSharingId.IsFailed)
                return Result.Fail("Cannot close Document Successfully.");

            return Result.Ok();
        }

        public async Task<Result<List<DepositionDocument>>> GetEnteredExhibits(Guid depostionId, ExhibitSortField? sortedField = null, SortDirection? sortDirection = null)
        {
            var includes = new[] { $"{ nameof(DepositionDocument.Document) }.{ nameof(Document.AddedBy) }" };
            Expression<Func<DepositionDocument, object>> orderBy = sortedField switch
            {
                ExhibitSortField.Name => x => x.Document.DisplayName,
                ExhibitSortField.Owner => x => x.Document.AddedBy.FirstName + x.Document.AddedBy.LastName,
                ExhibitSortField.SharedAt => x => x.Document.SharedAt,
                _ => x => x.Document.SharedAt,
            };

            Expression<Func<DepositionDocument, object>> orderByThen = x => x.Document.AddedBy.LastName;

            var depositionDocuments = await _depositionDocumentRepository.GetByFilterOrderByThen(
                orderBy,
                sortDirection ?? SortDirection.Descend,
                x => x.DepositionId == depostionId && x.Document.DocumentType == DocumentType.Exhibit,
                includes,
                sortedField == ExhibitSortField.Owner ? orderByThen : null);

            return Result.Ok(depositionDocuments.ToList());
        }

        // Check if Participant is owner of the document or if is Admin or CourReporter otherwise return false.
        public async Task<bool> ParticipantCanCloseDocument(Document document, Guid depositionId)
        {
            var currentUser = await _userService.GetCurrentUserAsync();
            if (currentUser.IsAdmin || currentUser.EmailAddress.Equals(document?.AddedBy?.EmailAddress))
                return true;

            var participantResult = await _depositionService.GetDepositionParticipantByEmail(depositionId, currentUser.EmailAddress);

            if (participantResult.IsFailed)
                return false;

            var role = participantResult.Value.Role;

            return role == ParticipantType.CourtReporter;
        }

        public async Task<bool> IsPublicDocument(Guid depositionId, Guid documentId)
        {
            var depositionDocument = await _depositionDocumentRepository.GetFirstOrDefaultByFilter(x => x.DepositionId == depositionId && x.DocumentId == documentId);
            return depositionDocument != null;
        }

        private async Task<Result> RemoveAnnotationEvents(Guid documentId)
        {
            return await _annotationEventService.RemoveUserDocumentAnnotations(documentId);
        }

        private async Task<Result> ClearDepositionDocumentSharingId(Guid depositionId)
        {
            return await _depositionService.ClearDepositionDocumentSharingId(depositionId);
        }

        public async Task<Result> RemoveDepositionTranscript(Guid depositionId, Guid documentId)
        {
            var include = new[] { $"{nameof(Deposition.Documents)}.{nameof(DepositionDocument.Document)}" };
            var deposition = await _depositionRepository.GetById(depositionId, include);
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Could not find any deposition with Id {depositionId}"));

            var depositionDocument = deposition.Documents.FirstOrDefault(d => d.DepositionId == depositionId && d.DocumentId == documentId);
            if (depositionDocument == null)
                return Result.Fail(new ResourceNotFoundError($"Could not find any document with Id {documentId}"));

            if (depositionDocument.Document.DocumentType != DocumentType.Transcription)
                return Result.Fail(new InvalidInputError($"Can not delete document with Id {documentId} this is not a transcript document"));

            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                try
                {
                    await _documentRepository.Remove(depositionDocument.Document);
                    await _depositionDocumentRepository.Remove(depositionDocument);
                    await _awsStorageService.DeleteObjectAsync(_documentsConfiguration.BucketName, depositionDocument.Document.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to delete documents");
                    Result.Fail(new ExceptionalError("Unable to delete documents", ex));
                }
            });

            if (transactionResult.IsFailed)
            {
                return transactionResult;
            }

            return Result.Ok();
        }
    }
}
