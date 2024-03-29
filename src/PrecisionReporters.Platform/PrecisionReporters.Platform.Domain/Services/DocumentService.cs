﻿using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using pdftron.FDF;
using pdftron.PDF;
using pdftron.SDF;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Helpers.Interfaces;
using PrecisionReporters.Platform.Domain.Mappers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Errors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IAwsStorageService _awsStorageService;
        private readonly IUserService _userService;
        private readonly IDocumentUserDepositionRepository _documentUserDepositionRepository;
        private readonly IPermissionService _permissionService;
        private readonly ITransactionHandler _transactionHandler;
        private readonly IDocumentRepository _documentRepository;
        private readonly IDepositionDocumentRepository _depositionDocumentRepository;
        private readonly ILogger<DocumentService> _logger;
        private readonly DocumentConfiguration _documentsConfiguration;
        private readonly IDepositionRepository _depositionRepository;
        private readonly IAnnotationEventRepository _annotationEventRepository;
        private readonly ISignalRDepositionManager _signalRNotificationManager;
        private readonly IMapper<AnnotationEvent, AnnotationEventDto, CreateAnnotationEventDto> _annotationEventMapper;
        private readonly IFileHelper _fileHelper;

        public DocumentService(IAwsStorageService awsStorageService, IOptions<DocumentConfiguration> documentConfigurations, ILogger<DocumentService> logger,
            IUserService userService, IDocumentUserDepositionRepository documentUserDepositionRepository,
            IPermissionService permissionService, ITransactionHandler transactionHandler,
            IDocumentRepository documentRepository, IDepositionDocumentRepository depositionDocumentRepository,
            IDepositionRepository depositionRepository, IAnnotationEventRepository annotationEventRepository,
            ISignalRDepositionManager signalRNotificationManager,
            IMapper<AnnotationEvent, AnnotationEventDto, CreateAnnotationEventDto> annotationEventMapper,
            IFileHelper fileHelper)
        {
            _awsStorageService = awsStorageService;
            _documentsConfiguration = documentConfigurations.Value ?? throw new ArgumentException(nameof(documentConfigurations));
            _logger = logger;
            _userService = userService;
            _documentUserDepositionRepository = documentUserDepositionRepository;
            _permissionService = permissionService;
            _transactionHandler = transactionHandler;
            _documentRepository = documentRepository;
            _depositionDocumentRepository = depositionDocumentRepository;
            _depositionRepository = depositionRepository;
            _annotationEventRepository = annotationEventRepository;
            _signalRNotificationManager = signalRNotificationManager;
            _annotationEventMapper = annotationEventMapper;
            _fileHelper = fileHelper;
        }

        public async Task<Result<Document>> UploadDocumentFile(KeyValuePair<string, FileTransferInfo> file, User user, string parentPath, DocumentType documentType)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Value.Name)}";

            var document = await UploadFileToStorage(file.Value, user, fileName, parentPath, documentType);
            if (document.IsFailed)
                return document;

            document.Value.FileKey = file.Key;
            return document;
        }

        public async Task<Result<string>> GenerateZipFile(List<DepositionDocument> depositionDocuments)
        {
            var zipName = $"{Guid.NewGuid()}{ApplicationConstants.ZipExtension}";
            var documents = depositionDocuments.Select(x => x.Document).ToList();

            try
            {
                foreach (var doc in documents)
                {
                    await using var fileStr = await _awsStorageService.GetObjectAsync(doc.FilePath, _documentsConfiguration.BucketName);
                    await _fileHelper.CreateFile(new FileTransferInfo() { FileStream = fileStr, Name = doc.DisplayName });
                }

                _fileHelper.GenerateZipFile(zipName, documents.Select(x => x.DisplayName).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Result.Ok(zipName);
        }

        public async Task<Result<Document>> UploadDocumentFile(FileTransferInfo file, User user, string parentPath, DocumentType documentType)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";

            var document = await UploadFileToStorage(file, user, fileName, parentPath, documentType);
            return document;
        }

        public async Task<Result<Document>> UploadExhibit(FileTransferInfo file, User user, string parentPath, DocumentType documentType)
        {
            var nonPdfExtensions = _documentsConfiguration.NonConvertToPdfExtensions.Contains(Path.GetExtension(file.Name)?.ToLower());
            var extension = !nonPdfExtensions ? ApplicationConstants.PDFExtension : Path.GetExtension(file.Name);
            var fileName = $"{Guid.NewGuid()}{extension}";

            var document = await UploadExhibitToStorage(file, user, fileName, parentPath, documentType);
            return document;
        }

        public async Task DeleteUploadedFiles(List<Document> uploadedDocuments)
        {
            foreach (var document in uploadedDocuments)
            {
                var deleteObjectResponse = await _awsStorageService.DeleteObjectAsync(_documentsConfiguration.BucketName, document.FilePath);
                if (deleteObjectResponse.IsFailed)
                    _logger.LogError($"Error while trying to delete document {document.Name} from {document.FilePath}. Storage response was {deleteObjectResponse.Errors[0]}");
            }
        }

        public Result ValidateFiles(List<FileTransferInfo> files)
        {
            if (files.Any(f => f.Length > _documentsConfiguration.MaxFileSize))
            {
                _logger.LogError("Error while trying to validate files: At least one of the files size exceeds the allowed limit");
                return Result.Fail(new InvalidInputError("Exhibit size exceeds the allowed limit"));
            }

            if (files.Any(f => !_documentsConfiguration.AcceptedFileExtensions.Contains(Path.GetExtension(f.Name)?.ToLower())))
            {
                _logger.LogError("Error while trying to validate files: At least one of the files extension is not valid");
                return Result.Fail(new InvalidInputError("Failed to upload the file. Please try again"));
            }

            return Result.Ok();
        }

        public Result ValidateFile(FileTransferInfo file)
        {
            if (file.Length > _documentsConfiguration.MaxFileSize)
            {
                _logger.LogError("Error while trying to validate file: File size exceeds the allowed limit");
                return Result.Fail(new InvalidInputError("Exhibit size exceeds the allowed limit"));
            }

            if (!_documentsConfiguration.AcceptedFileExtensions.Contains(Path.GetExtension(file.Name)?.ToLower()))
            {
                _logger.LogError($"Error while trying to validate file: File extension \"{Path.GetExtension(file.Name)}\" is not valid");
                return Result.Fail(new InvalidInputError("Failed to upload the file. Please try again"));
            }

            return Result.Ok();
        }

        private Result ValidateTranscriptions(List<FileTransferInfo> files)
        {
            if (files.Any(f => f.Length > _documentsConfiguration.MaxFileSize))
            {
                _logger.LogError("Error while trying to validate transcriptions: At least one of the files size exceeds the allowed limit");
                return Result.Fail(new InvalidInputError("Exhibit size exceeds the allowed limit"));
            }

            if (files.Any(f => !_documentsConfiguration.AcceptedTranscriptionExtensions.Contains(Path.GetExtension(f.Name)?.ToLower())))
            {
                _logger.LogError("Error while trying to validate transcriptions: At least one of the files extension is not valid");
                return Result.Fail(new InvalidInputError("Extension of the file is not correct"));
            }

            return Result.Ok();
        }

        public async Task<Result> UploadDocuments(Guid id, string identity, List<FileTransferInfo> files, string folder, DocumentType documentType)
        {
            var userResult = await _userService.GetUserByEmail(identity);
            if (userResult.IsFailed)
                return userResult.ToResult();
            var include = new[] { nameof(Deposition.DocumentUserDepositions) };
            var depositionResult = await _depositionRepository.GetById(id, include);
            if (depositionResult == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            var fileValidationResult = ValidateFiles(files);
            if (fileValidationResult.IsFailed)
                return fileValidationResult;

            var uploadedDocuments = new List<DocumentUserDeposition>();
            foreach (var file in files)
            {
                // TODO: Use depositionId for bucket folder
                var documentResult = await UploadExhibit(file, userResult.Value, $"{depositionResult.CaseId}/{depositionResult.Id}/{folder}", documentType);
                if (documentResult.IsFailed)
                {
                    _logger.LogError(new Exception(documentResult.Errors.First().Message), "Unable to load one or more documents to storage for user: {0}", userResult.Value.EmailAddress);
                    _logger.LogInformation("Removing uploaded documents");
                    await DeleteUploadedFiles(uploadedDocuments.Select(d => d.Document).ToList());
                    return documentResult.ToResult();
                }
                uploadedDocuments.Add(new DocumentUserDeposition { Deposition = depositionResult, Document = documentResult.Value, User = userResult.Value });
            }
            var documentCreationResult = Result.Ok();

            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                try
                {
                    uploadedDocuments = await _documentUserDepositionRepository.CreateRange(uploadedDocuments);
                    foreach (var document in uploadedDocuments)
                    {
                        var documentPermissionResult = await _permissionService.AddUserRole(document.UserId, document.DocumentId, ResourceType.Document, RoleName.DocumentOwner);
                        if (documentPermissionResult.IsFailed)
                        {
                            _logger.LogError("Unable to create documents permissions");
                            await DeleteUploadedFiles(uploadedDocuments.Select(d => d.Document).ToList());
                            documentCreationResult = Result.Fail(new Error("Unable to create document permissions"));
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to add documents to deposition");
                    await DeleteUploadedFiles(uploadedDocuments.Select(d => d.Document).ToList());
                    documentCreationResult = Result.Fail(new ExceptionalError("Unable to add documents to deposition", ex));
                }
            });

            if (transactionResult.IsFailed)
            {
                return transactionResult;
            }

            return documentCreationResult;
        }

        public async Task<Result> UploadTranscriptions(Guid id, List<FileTransferInfo> files)
        {
            var currentUser = await _userService.GetCurrentUserAsync();
            var includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants),
                nameof(Deposition.Case), nameof(Deposition.AddedBy),nameof(Deposition.Caption)};

            var depositionResult = await _depositionRepository.GetById(id, includes);
            if (depositionResult == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {id} not found."));

            var fileValidationResult = ValidateTranscriptions(files);
            if (fileValidationResult.IsFailed)
                return fileValidationResult;

            var uploadedDocuments = new List<DepositionDocument>();
            var folder = DocumentType.Transcription.GetDescription();
            foreach (var file in files)
            {
                var documentResult = await UploadDocumentFile(file, currentUser, $"{depositionResult.CaseId}/{depositionResult.Id}/{folder}", DocumentType.Transcription);
                if (documentResult.IsFailed)
                {
                    _logger.LogError(documentResult.Errors.First().Message, "Unable to load one or more documents to storage");
                    _logger.LogInformation("Removing uploaded documents");
                    await DeleteUploadedFiles(uploadedDocuments.Select(d => d.Document).ToList());
                    return documentResult.ToResult();
                }
                uploadedDocuments.Add(new DepositionDocument { Deposition = depositionResult, Document = documentResult.Value });
            }
            var documentCreationResult = Result.Ok();
            await _transactionHandler.RunAsync(async () =>
            {
                try
                {
                    uploadedDocuments = await _depositionDocumentRepository.CreateRange(uploadedDocuments);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to add documents to deposition");
                    await DeleteUploadedFiles(uploadedDocuments.Select(d => d.Document).ToList());
                    documentCreationResult = Result.Fail(new ExceptionalError("Unable to add documents to deposition", ex));
                }
            });

            return documentCreationResult;
        }

        public async Task<Result<List<Document>>> GetExhibitsForUser(Guid depositionId, string identity)
        {
            var userResult = await _userService.GetUserByEmail(identity);
            if (userResult.IsFailed)
                return userResult.ToResult<List<Document>>();

            var includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants),
                nameof(Deposition.Case), nameof(Deposition.AddedBy),nameof(Deposition.Caption)};

            var depositionResult = await _depositionRepository.GetById(depositionId, includes);
            if (depositionResult == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {depositionId} not found."));

            var documentUserDeposition = await _documentUserDepositionRepository.GetByFilter(x => x.DepositionId == depositionId && x.UserId == userResult.Value.Id && x.Document.DocumentType == DocumentType.Exhibit, new[] { nameof(DocumentUserDeposition.Document) });

            return Result.Ok(documentUserDeposition.Select(d => d.Document).ToList());
        }

        public async Task<Result<string>> GetFileSignedUrl(Guid documentId)
        {
            var document = await _documentRepository.GetFirstOrDefaultByFilter(x => x.Id == documentId);
            if (document == null)
                return Result.Fail(new ResourceNotFoundError($"Could not find any document with Id {documentId}"));

            var signedUrl = GetFileSignedUrl(document);

            return Result.Ok(signedUrl.Value);
        }

        public Result<string> GetFileSignedUrl(Document document)
        {
            var expirationDate = DateTime.UtcNow.AddHours(_documentsConfiguration.PreSignedUrlValidHours);
            var signedUrl = _awsStorageService.GetFilePublicUri(document.FilePath,
                _documentsConfiguration.BucketName, expirationDate, document.DisplayName);

            return Result.Ok(signedUrl);
        }

        public async Task<Result<List<string>>> GetFileSignedUrl(Guid depositionId, List<Guid> documentIds)
        {
            Expression<Func<DepositionDocument, bool>> filter = x => x.DepositionId == depositionId && documentIds.Contains(x.DocumentId);

            var depositionDocument = await _depositionDocumentRepository.GetByFilter(filter, new[] { nameof(DepositionDocument.Document) });
            if (depositionDocument == null)
                return Result.Fail(new ResourceNotFoundError($"Could not find any document for deposition {depositionId}"));

            if (depositionDocument.Count > 1)
            {
                var deposition = await _depositionRepository.GetById(depositionId, new[] { nameof(Deposition.Case), nameof(Deposition.Participants) });

                var parentPath = $"{deposition.CaseId}/{deposition.Id}";
                var generateZipFileResult = await GenerateZipFile(depositionDocument);
                var documents = depositionDocument.Select(x => x.Document).ToList();
                var documentType = documents.FirstOrDefault()?.DocumentType;

                var uploadZipFileToStorageResult = await UploadZipFileToStorage(parentPath, generateZipFileResult.Value, deposition, documentType);

                var signedZipFileUrl = GetFileSignedUrl(uploadZipFileToStorageResult.Value);
                return Result.Ok(new List<string>() { signedZipFileUrl.Value });
            }

            var signedUrl = await GetFileSignedUrl(documentIds.FirstOrDefault());
            return Result.Ok(new List<string>() { signedUrl.Value });
        }

        public async Task<Result<string>> GetCannedPrivateURL(Guid depositionId, Guid documentId)
        {
            var depositionDocument = await _depositionDocumentRepository.GetFirstOrDefaultByFilter(x => x.DepositionId == depositionId && x.DocumentId == documentId, new[] { nameof(DepositionDocument.Document) });
            if (depositionDocument == null)
                return Result.Fail(new ResourceNotFoundError($"Could not find any document with Id {documentId}"));

            if (depositionDocument.Document.DocumentType == DocumentType.DraftTranscription)
                return GetFileSignedUrl(depositionDocument.Document);

            return GetCannedPrivateURL(depositionDocument.Document);
        }

        public async Task<Result<string>> GetCannedPrivateURL(Guid documentId)
        {
            var document = await _documentRepository.GetFirstOrDefaultByFilter(x => x.Id == documentId);
            if (document == null)
                return Result.Fail(new ResourceNotFoundError($"Could not find any document with Id {documentId}"));

            var signedUrl = GetCannedPrivateURL(document);

            return Result.Ok(signedUrl.Value);
        }

        public Result<string> GetCannedPrivateURL(Document document)
        {
            var expirationDate = DateTime.UtcNow.AddHours(_documentsConfiguration.PreSignedUrlValidHours);
            var signedUrl = _awsStorageService.GetCannedPrivateURL(document.FilePath, expirationDate, _documentsConfiguration.CloudfrontPrivateKey, _documentsConfiguration.CloudfrontXmlKey, _documentsConfiguration.CloudfrontPolicyStatement);

            return Result.Ok(signedUrl);
        }

        public async Task<Result> Share(Guid id, string userEmail)
        {
            var userResult = await _userService.GetUserByEmail(userEmail);
            if (userResult.IsFailed)
                return userResult;

            var documentUserDepositionResult = await _documentUserDepositionRepository.GetFirstOrDefaultByFilter(x => x.DocumentId == id && x.UserId == userResult.Value.Id, new[] { nameof(DocumentUserDeposition.Document) });
            if (documentUserDepositionResult == null)
                return Result.Fail(new Error($"Could not find any document with Id {id} for user {userEmail}"));

            var includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants),
                nameof(Deposition.Case), nameof(Deposition.AddedBy),nameof(Deposition.Caption)};

            var depositionResult = await _depositionRepository.GetById(documentUserDepositionResult.DepositionId, includes);
            if (depositionResult == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {documentUserDepositionResult.DepositionId} not found."));

            if (depositionResult.SharingDocumentId.HasValue)
                return Result.Fail(new ResourceConflictError("Can't share document while another document is being shared."));

            depositionResult.SharingDocumentId = id;

            // Save ShareAt information as soon as the document is been shared
            var document = await _documentRepository.GetById(id);
            document.SharedAt = DateTime.UtcNow;
            await _documentRepository.Update(document);

            return Result.Ok(await _depositionRepository.Update(depositionResult));
        }

        public async Task<Result<Document>> GetDocument(Guid id)
        {
            var document = await _documentRepository.GetById(id, new[] { nameof(Document.AddedBy) });
            if (document == null)
                return Result.Fail(new ResourceNotFoundError("Document not found"));
            return Result.Ok(document);
        }

        public async Task<Result> UpdateDocument(Document document, DepositionDocument depositionDocument, string identity, string temporalPath, string folder)
        {
            var userResult = await _userService.GetUserByEmail(identity);
            if (userResult.IsFailed)
                return userResult.ToResult();

            var include = new[] { nameof(Deposition.DocumentUserDepositions) };
            var depositionResult = await _depositionRepository.GetById(depositionDocument.DepositionId, include);
            if (depositionResult == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {depositionDocument.DepositionId} not found."));

            var parentPath = $"{depositionResult.CaseId}/{depositionResult.Id}/{folder}";
            var documentResult = await SaveDocumentWithAnnotationsToS3(document, temporalPath, parentPath);
            if (documentResult.IsFailed)
            {
                _logger.LogError(new Exception(documentResult.Errors.First().Message), "Unable to update the document to storage");
                return documentResult;
            }

            // When original file is not a pdf we need to make sure we remove it from S3 since in that case it won't get overriden
            if (document.Type != ApplicationConstants.PDFExtension)
            {
                await _awsStorageService.DeleteObjectAsync(_documentsConfiguration.BucketName, document.FilePath);
            }

            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                try
                {
                    // Update document file Name and Path with the PDF extension that PDFTron response
                    var updateDocument = await _documentRepository.GetById(depositionDocument.DocumentId);
                    var fileName = $"{Path.GetFileNameWithoutExtension(document.FilePath)}_Entered{ApplicationConstants.PDFExtension}";
                    updateDocument.Name = fileName;
                    updateDocument.DisplayName = $"{Path.GetFileNameWithoutExtension(document.DisplayName)}{ApplicationConstants.PDFExtension}";

                    updateDocument.FilePath = $"files/{parentPath}/{fileName}";
                    updateDocument.Type = ApplicationConstants.PDFExtension;
                    await _documentRepository.Update(updateDocument);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to delete user documents");
                    Result.Fail(new ExceptionalError("Unable to delete user documents", ex));
                }
            });

            if (transactionResult.IsFailed)
            {
                return transactionResult;
            }

            return Result.Ok();
        }

        private async Task<Result<Document>> UploadFileToStorage(FileTransferInfo file, User user, string fileName, string parentPath, DocumentType documentType)
        {
            var documentKeyName = $"/{parentPath}/{fileName}";
            var uploadedDocument = await _awsStorageService.UploadMultipartAsync(documentKeyName, file, _documentsConfiguration.BucketName);
            if (uploadedDocument.IsFailed)
                return uploadedDocument.ToResult<Document>();

            var document = new Document
            {
                Type = Path.GetExtension(file.Name),
                AddedBy = user,
                Name = fileName,
                DisplayName = file.Name,
                FilePath = documentKeyName,
                Size = file.Length,
                DocumentType = documentType
            };

            return Result.Ok(document);
        }

        private async Task<Result<Document>> UploadZipFileToStorage(string parentPath, string zipName, Deposition deposition, DocumentType? documentType)
        {
            var documentKeyName = $"files/{parentPath}/{zipName}";
            var witness = deposition.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
            var uploadZipFileResult = await _awsStorageService.UploadObjectFromFileAsync(zipName, documentKeyName, _documentsConfiguration.BucketName);
            if (uploadZipFileResult.IsFailed)
                return uploadZipFileResult;

            var displayName = $"{deposition.Case.Name}-{witness?.GetFullName()}{ApplicationConstants.ZipExtension}";
            if (documentType != null)
            {
                var documentTypeDescription = documentType == DocumentType.Exhibit ? "Exhibits" : "Transcripts";

                displayName = $"{deposition.Case.Name}-{witness?.GetFullName()}-{documentTypeDescription}{ApplicationConstants.ZipExtension}";
            }

            var document = new Document() { FilePath = documentKeyName, Name = zipName, DisplayName = displayName };

            if (File.Exists(zipName))
                File.Delete(zipName);

            return Result.Ok(document);
        }

        public async Task<Result<Document>> GetDocumentById(Guid documentId, string[] include = null)
        {
            var document = await _documentRepository.GetById(documentId, include);
            if (document == null)
                return Result.Fail(new ResourceNotFoundError($"Document with Id {documentId} "));

            return Result.Ok(document);
        }

        public async Task<Result> AddAnnotation(Guid depositionId, AnnotationEvent annotation)
        {
            var currentUser = await _userService.GetCurrentUserAsync();

            var depositionResult = await _depositionRepository.GetById(depositionId);
            if (depositionResult == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {depositionId} not found."));

            if (!depositionResult.SharingDocumentId.HasValue)
                return Result.Fail(new ResourceNotFoundError($"There is no shared document for deposition {depositionId}"));

            var documentResult = await GetDocumentById(depositionResult.SharingDocumentId.Value);
            if (documentResult.IsFailed)
                return documentResult;

            annotation.DocumentId = depositionResult.SharingDocumentId.Value;
            annotation.Author = currentUser;
            annotation.CreationDate = DateTime.UtcNow;

            var notificationDto = new NotificationDto
            {
                Action = NotificationAction.Create,
                EntityType = NotificationEntity.Annotation,
                Content = _annotationEventMapper.ToDto(annotation)
            };

            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                await _annotationEventRepository.Create(annotation);
                await _signalRNotificationManager.SendNotificationToDepositionMembers(depositionId, notificationDto);
            });

            return Result.Ok();
        }

        public async Task<Result> RemoveDepositionUserDocuments(Guid documentId)
        {
            var documentUser = await _documentUserDepositionRepository.GetFirstOrDefaultByFilter(x => x.DocumentId == documentId);
            if (documentUser == null)
                return Result.Fail(new ResourceNotFoundError());

            await _documentUserDepositionRepository.Remove(documentUser);
            return Result.Ok();
        }

        public async Task<Result> ShareEnteredExhibit(Guid depositionId, Guid documentId)
        {
            var userResult = await _userService.GetCurrentUserAsync();
            var includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants),
                nameof(Deposition.Case), nameof(Deposition.AddedBy),nameof(Deposition.Caption)};
            var depositionResult = await _depositionRepository.GetById(depositionId, includes);
            if (depositionResult == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {depositionId} not found."));
            if (depositionResult.SharingDocumentId.HasValue)
                return Result.Fail(new ResourceConflictError("Can't share document while another document is being shared."));

            var document = await _documentRepository.GetById(documentId);
            if (document == null)
                return Result.Fail(new Error($"Could not find any document with Id {documentId}"));

            return await _transactionHandler.RunAsync<Deposition>(async () =>
            {
                try
                {
                    depositionResult.SharingDocumentId = document.Id;
                    document.SharedAt = DateTime.UtcNow;
                    _logger.LogDebug($"{nameof(DocumentService)}.{nameof(ShareEnteredExhibit)}: Updating document with Id '{documentId}'");
                    await _documentRepository.Update(document);

                    _logger.LogDebug($"{nameof(DocumentService)}.{nameof(ShareEnteredExhibit)}: Updating deposition with Id '{depositionId}'");
                    return Result.Ok(await _depositionRepository.Update(depositionResult));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to share exhibit with Id '{0}' for deposition '{1}' for user '{2}'", documentId, depositionId, userResult.EmailAddress);
                    return Result.Fail(new ExceptionalError("Unable to share exhibit", ex));
                }
            });
        }

        public async Task<Result> RemoveDepositionDocument(Guid depositionId, Guid documentId)
        {
            var include = new[] { nameof(Document.DocumentUserDepositions) };
            var document = await _documentRepository.GetFirstOrDefaultByFilter(x => x.Id == documentId, include);
            if (document == null)
                return Result.Fail(new Error($"Could not find any document with Id {documentId}"));

            var deposition = await _depositionRepository.GetById(depositionId);
            if (deposition == null)
                return Result.Fail(new Error($"Could not find any deposition with Id {depositionId}"));

            if (deposition.SharingDocumentId.Equals(documentId))
                return Result.Fail(new Error($"Could not delete and Exhibits while is being shared. Exhibit id {documentId}"));

            var documentUser = document.DocumentUserDepositions.FirstOrDefault(x => x.DocumentId == documentId);
            if (documentUser == null)
                return Result.Fail(new Error($"Could not find any user document with Id {documentId}"));

            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                try
                {
                    await _documentUserDepositionRepository.Remove(documentUser);
                    await _documentRepository.Remove(document);
                    await _awsStorageService.DeleteObjectAsync(_documentsConfiguration.BucketName, document.FilePath);
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

        private async Task<Result> SaveDocumentWithAnnotationsToS3(Document document, string temporalPath, string parentPath)
        {
            using var fdfDoc = new FDFDoc();
            var filePath = temporalPath + document.Name;
            var annotations = await _annotationEventRepository.GetByFilter(x => x.DocumentId == document.Id);
            await using var fileStr = await _awsStorageService.GetObjectAsync(document.FilePath, _documentsConfiguration.BucketName);

            await using (Stream file = File.Create(filePath))
            {
                await fileStr.CopyToAsync(file);
            }
            using var pdfDoc = new PDFDoc(filePath);

            // Merge annotation into FDFDoc and then save into the new PDF
            foreach (var annotation in annotations)
            {
                fdfDoc.MergeAnnots(annotation.Details);
            }
            pdfDoc.FDFMerge(fdfDoc);

            await using var streamDoc = new MemoryStream();
            try
            {
                pdfDoc.Save(streamDoc, SDFDoc.SaveOptions.e_linearized);
                var fileName = $"{Path.GetFileNameWithoutExtension(document.FilePath)}_Entered{ApplicationConstants.PDFExtension}";
                var s3FilePath = $"files/{parentPath}/{fileName}";
                var result = await _awsStorageService.UploadObjectFromStreamAsync(s3FilePath, streamDoc, _documentsConfiguration.BucketName);
                await _awsStorageService.DeleteObjectAsync(_documentsConfiguration.BucketName, document.FilePath);
                File.Delete(filePath);

                return result;
            }
            catch (Exception ex)
            {
                return Result.Fail(new UnexpectedError($"Fail to save document file into S3 storage with exception: {ex}"));
            }
        }

        public async Task<Result<List<FileSignedDto>>> GetFrontEndContent()
        {
            try
            {
                var s3Objects = await _awsStorageService.GetAllObjectInBucketAsync(_documentsConfiguration.FrontEndContentBucket);
                List<FileSignedDto> filesList = new List<FileSignedDto>();
                foreach (var s3Object in s3Objects)
                {
                    filesList.Add(GetFileDto(s3Object.Key, s3Object.BucketName));
                }

                return Result.Ok(filesList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting files fromt bucket: {_documentsConfiguration.FrontEndContentBucket}");
                return Result.Fail(new ExceptionalError("Unable to get frontend files", ex));
            }

        }

        private FileSignedDto GetFileDto(string key, string bucketName)
        {
            var expirationDate = DateTime.UtcNow.AddHours(_documentsConfiguration.PreSignedUrlValidHours);
            return new FileSignedDto
            {
                Name = Path.GetFileNameWithoutExtension(key),
                Url = _awsStorageService.GetFilePublicUri(key, bucketName, expirationDate, null, true)
            };
        }

        private async Task<Result<Document>> UploadExhibitToStorage(FileTransferInfo file, User user, string fileName, string parentPath, DocumentType documentType)
        {
            var documentKeyName = $"files/{parentPath}/{fileName}";
            var pathFiles = new List<string> { file.Name };
            var type = Path.GetExtension(file.Name);

            try
            {
                var nonPdfExtensions = _documentsConfiguration.NonConvertToPdfExtensions.Contains(Path.GetExtension(file.Name)?.ToLower());
                if (!nonPdfExtensions)
                {
                    var pathPDF = _fileHelper.ConvertFileToPDF(file).Result;
                    pathFiles.Add(pathPDF);
                    var pathOptimizedPDF = _fileHelper.OptimizePDF(pathPDF);
                    pathFiles.Add(pathOptimizedPDF);

                    var uploadedDocumentFromPath = await _awsStorageService.UploadAsync(documentKeyName, pathOptimizedPDF, _documentsConfiguration.BucketName);
                    if (uploadedDocumentFromPath.IsFailed)
                        return uploadedDocumentFromPath.ToResult<Document>();

                    file.Length = uploadedDocumentFromPath.Value.Length;
                }
                else
                {
                    var uploadedDocumentFromFile = await _awsStorageService.UploadMultipartAsync(documentKeyName, file, _documentsConfiguration.BucketName);
                    if (uploadedDocumentFromFile.IsFailed)
                        return uploadedDocumentFromFile.ToResult<Document>();
                }

                var document = new Document
                {
                    Type = type,
                    AddedBy = user,
                    Name = fileName,
                    DisplayName = file.Name,
                    FilePath = documentKeyName,
                    Size = file.Length,
                    DocumentType = documentType
                };

                return Result.Ok(document);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Result.Fail(new Error(ex.Message));
            }
            finally
            {
                foreach (var path in pathFiles)
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
            }
        }

        /// <summary>
        /// Generate a presigned URL that can be used to access the file named
        /// in the ojbectKey parameter for the amount of time specified in the
        /// duration parameter.
        /// </summary>
        /// <param name="preSignedUploadUrl"></param>
        /// <returns></returns>
        public async Task<Result<PreSignedUrlDto>> GetPreSignedUrlUploadExhibit(PreSignedUploadUrlDto preSignedUploadUrl)
        {
            var deposition = await _depositionRepository.GetById(preSignedUploadUrl.DepositionId);
            if (deposition == null)
            {
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {preSignedUploadUrl.DepositionId} not found."));
            }
            var extension = Path.GetExtension(preSignedUploadUrl.FileName);
            var keyFileName = $"{Guid.NewGuid()}{extension}";
            var folder = "temp-" + DocumentType.Exhibit.GetDescription().ToLower();
            var documentKeyName = $"{folder}/{deposition.CaseId}/{deposition.Id}/{keyFileName}";
            var metadata = await GenerateUploadMetadata(preSignedUploadUrl, deposition, extension);
            var expirationDate = DateTime.UtcNow.AddSeconds(_documentsConfiguration.PreSignedUploadUrlValidSeconds);
            var urlResult = _awsStorageService.GetPreSignedPutUrl(documentKeyName, _documentsConfiguration.BucketName, expirationDate, metadata);

            if (urlResult.IsFailed)
            {
                var msg = $"Error getting Presigned Url from Amazon S3 Services. {urlResult.Errors.First().Message}";
                return Result.Fail(new Error(msg));
            }

            return Result.Ok(urlResult.Value);
        }

        private async Task<Dictionary<string, string>> GenerateUploadMetadata(PreSignedUploadUrlDto preSignedUploadUrl, Deposition deposition, string extension)
        {
            var user = await _userService.GetCurrentUserAsync();
            var metadata = new Dictionary<string, string>
            {
                { ApplicationConstants.UserIdExhibitsMetadata , user.Id.ToString() },
                { ApplicationConstants.DepositionIdExhibitsMetadata , deposition.Id.ToString() },
                { ApplicationConstants.CaseIdExhibitsMetadata, deposition.CaseId.ToString() },
                { ApplicationConstants.DisplayNameExhibitsMetadata, preSignedUploadUrl.FileName },
                { ApplicationConstants.TypeExhibitsMetadata, extension },
                { ApplicationConstants.DocumentTypeExhibitsMetadata, DocumentType.Exhibit.ToString()},
                { ApplicationConstants.ResourceIdExhibitsMetadata, preSignedUploadUrl.ResourceId}
            };

            return metadata;
        }
    }
}
