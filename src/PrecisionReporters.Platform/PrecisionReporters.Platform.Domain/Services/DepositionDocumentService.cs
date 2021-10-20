using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Errors;
using PrecisionReporters.Platform.Shared.Extensions;
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
        private readonly ISignalRDepositionManager _signalRNotificationManager;

        public DepositionDocumentService(IDepositionDocumentRepository depositionDocumentRepository,
            IAnnotationEventService annotationEventService,
            IDocumentService documentService,
            IDepositionService depositionService,
            IUserService userService,
            ITransactionHandler transactionHandler,
            IDepositionRepository depositionRepository,
            IDocumentRepository documentRepository,
            IAwsStorageService awsStorageService,
            IOptions<DocumentConfiguration> documentConfigurations,
            ILogger<DepositionDocumentService> logger,
            ISignalRDepositionManager signalRNotificationManager)
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
            _signalRNotificationManager = signalRNotificationManager;
        }

        public async Task<Result> CloseStampedDepositionDocument(Document document, DepositionDocument depositionDocument, string identity, string temporalPath)
        {
            var canCloseDocument = await ParticipantCanCloseDocument(document, depositionDocument.DepositionId);
            if (!canCloseDocument)
            {
                _logger.LogError($"{nameof(DepositionDocumentService)}.{nameof(CloseStampedDepositionDocument)}: Current user cannot close document with Id '{document.Id}' on deposition '{depositionDocument.DepositionId}'");
                return Result.Fail(new ForbiddenError());
            }

            DepositionDocument newDepositionDocument = null;
            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                newDepositionDocument = await _depositionDocumentRepository.Create(depositionDocument);
                // Update document in S3 and Delete entry from DocumentUserDepositions table
                if (!_documentsConfiguration.NonConvertToPdfExtensions.Contains(document.Type))
                {
                    var uploadResult = await _documentService.UpdateDocument(document, newDepositionDocument, identity, temporalPath, DocumentType.Exhibit.GetDescription());
                    if (uploadResult.IsFailed)
                    {
                        _logger.LogError($"{nameof(DepositionDocumentService)}.{nameof(CloseStampedDepositionDocument)}: Failed updating document '{document.Id}': {uploadResult.GetErrorMessage()}");
                        return uploadResult.ToResult<bool>();
                    }
                }
                var removeUserDocumentResult = await _documentService.RemoveDepositionUserDocuments(depositionDocument.DocumentId);
                if (removeUserDocumentResult.IsFailed)
                {
                    _logger.LogError($"{nameof(DepositionDocumentService)}.{nameof(CloseStampedDepositionDocument)}: Failed removing document '{depositionDocument.DocumentId}': {removeUserDocumentResult.GetErrorMessage()}");
                    return removeUserDocumentResult.ToResult<bool>();
                }

                var removeAnnotationsResult = await _annotationEventService.RemoveUserDocumentAnnotations(depositionDocument.DocumentId);
                if (removeAnnotationsResult.IsFailed)
                {
                    _logger.LogError($"{nameof(DepositionDocumentService)}.{nameof(CloseStampedDepositionDocument)}: Failed removing annotations on document '{depositionDocument.DocumentId}': {removeAnnotationsResult.GetErrorMessage()}");
                    return removeAnnotationsResult.ToResult<bool>();
                }

                var clearSharingIdResult = await _depositionService.ClearDepositionDocumentSharingId(depositionDocument.DepositionId);
                if (clearSharingIdResult.IsFailed)
                {
                    _logger.LogError($"{nameof(DepositionDocumentService)}.{nameof(CloseStampedDepositionDocument)}: Failed clearing SharingId on deposition '{depositionDocument.DepositionId}': {clearSharingIdResult.GetErrorMessage()}");
                    return clearSharingIdResult.ToResult<bool>();
                }

                await _signalRNotificationManager.SendNotificationToDepositionMembers(depositionDocument.DepositionId, new NotificationDto
                {
                    Action = NotificationAction.Close,
                    EntityType = NotificationEntity.Exhibit,
                    Content = document.Id
                });

                return Result.Ok(true);
            });

            if (transactionResult.IsFailed)
            {
                _logger.LogError($"{nameof(DepositionDocumentService)}.{nameof(CloseStampedDepositionDocument)}: Failed for document with Id '{document.Id}' on deposition '{depositionDocument.DepositionId}': {transactionResult.GetErrorMessage()}");
            }

            return transactionResult;
        }

        public async Task<Result> CloseDepositionDocument(Document document, Guid depositionId)
        {
            var canCloseDocument = await ParticipantCanCloseDocument(document, depositionId);
            if (!canCloseDocument)
            {
                _logger.LogError($"{nameof(DepositionDocumentService)}.{nameof(CloseDepositionDocument)}: Current user cannot close document with Id '{document.Id}' on deposition '{depositionId}'");
                return Result.Fail(new ForbiddenError());
            }

            return await _transactionHandler.RunAsync<Deposition>(async () =>
            {
                var removedAnnotationsResult = await _annotationEventService.RemoveUserDocumentAnnotations(document.Id);
                if (removedAnnotationsResult.IsFailed)
                {
                    _logger.LogError($"{nameof(DepositionDocumentService)}.{nameof(CloseDepositionDocument)}: Failed removing annotations on document '{document.Id}': {removedAnnotationsResult.GetErrorMessage()}");
                    return Result.Fail("Cannot close Document Successfully.");
                }

                var removedSharingIdResult = await _depositionService.ClearDepositionDocumentSharingId(depositionId);
                if (removedSharingIdResult.IsFailed)
                {
                    _logger.LogError($"{nameof(DepositionDocumentService)}.{nameof(CloseDepositionDocument)}: Failed clearing SharingId on deposition '{depositionId}': {removedSharingIdResult.GetErrorMessage()}");
                    return Result.Fail("Cannot close Document Successfully.");
                }

                await _signalRNotificationManager.SendNotificationToDepositionMembers(depositionId, new NotificationDto
                {
                    Action = NotificationAction.Close,
                    EntityType = NotificationEntity.Exhibit,
                    Content = document.Id
                });

                return Result.Ok();
            });
        }

        public async Task<Result<List<DepositionDocument>>> GetEnteredExhibits(Guid depositionId, ExhibitSortField? sortedField = null, SortDirection? sortDirection = null)
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
                x => x.DepositionId == depositionId && x.Document.DocumentType == DocumentType.Exhibit,
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

        public async Task<Result> BringAllToMe(Guid depositionId, BringAllToMeDto bringAllToMeDto)
        {
            var deposition = await _depositionRepository.GetById(depositionId);
            if (deposition == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition not found with ID: {depositionId}"));

            var user = await _userService.GetCurrentUserAsync();

            bringAllToMeDto.UserId = user?.Id;

            var notification = new NotificationDto()
            {
                Action = NotificationAction.Create,
                EntityType = NotificationEntity.BringAllTo,
                Content = bringAllToMeDto
            };
            await _signalRNotificationManager.SendNotificationToDepositionMembers(depositionId, notification);
            return Result.Ok();
        }

        public async Task<string> GetDocumentStampLabel(Guid documentId)
        {
            var depositionDocument = await _depositionDocumentRepository.GetFirstOrDefaultByFilter(d => d.DocumentId == documentId);
            return depositionDocument?.StampLabel;
        }
    }
}
