using FluentResults;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IAwsStorageService _awsStorageService;
        private readonly IUserService _userService;
        private readonly IDepositionService _depositionService;
        private readonly IDocumentUserDepositionRepository _documentUserDepositionRepository;
        private readonly IPermissionService _permissionService;
        private readonly ITransactionHandler _transactionHandler;
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<DocumentService> _logger;
        private readonly DocumentConfiguration _documentsConfiguration;

        public DocumentService(IAwsStorageService awsStorageService, IOptions<DocumentConfiguration> documentConfigurations, ILogger<DocumentService> logger,
            IUserService userService, IDepositionService depositionService, IDocumentUserDepositionRepository documentUserDepositionRepository,
            IPermissionService permissionService, ITransactionHandler transactionHandler, IDocumentRepository documentRepository)
        {
            _awsStorageService = awsStorageService;
            _documentsConfiguration = documentConfigurations.Value ?? throw new ArgumentException(nameof(documentConfigurations));
            _logger = logger;
            _userService = userService;
            _depositionService = depositionService;
            _documentUserDepositionRepository = documentUserDepositionRepository;
            _permissionService = permissionService;
            _transactionHandler = transactionHandler;
            _documentRepository = documentRepository;
        }

        public async Task<Result<Document>> UploadDocumentFile(KeyValuePair<string, FileTransferInfo> file, User user, string parentPath)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Value.Name)}";

            var document = await UploadFileToStorage(file.Value, user, fileName, parentPath);
            if (document.IsFailed)
                return document;

            document.Value.FileKey = file.Key;
            return document;
        }

        public async Task<Result<Document>> UploadDocumentFile(FileTransferInfo file, User user, string parentPath)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";

            var document = await UploadFileToStorage(file, user, fileName, parentPath);
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
                return Result.Fail(new InvalidInputError("Exhibit size exceeds the allowed limit"));

            if (files.Any(f => !_documentsConfiguration.AcceptedFileExtensions.Contains(Path.GetExtension(f.Name))))
                return Result.Fail(new InvalidInputError("Failed to upload the file. Please try again"));

            return Result.Ok();
        }

        public Result ValidateFile(FileTransferInfo file)
        {
            if (file.Length > _documentsConfiguration.MaxFileSize)
                return Result.Fail(new InvalidInputError("Exhibit size exceeds the allowed limit"));

            if (!_documentsConfiguration.AcceptedFileExtensions.Contains(Path.GetExtension(file.Name)))
                return Result.Fail(new InvalidInputError("Failed to upload the file. Please try again"));

            return Result.Ok();
        }

        public async Task<Result> UploadDocuments(Guid id, string identity, List<FileTransferInfo> files)
        {
            var userResult = await _userService.GetUserByEmail(identity);
            if (userResult.IsFailed)
                return userResult.ToResult();

            var depositionResult = await _depositionService.GetDepositionByIdWithDocumentUsers(id);
            if (depositionResult.IsFailed)
                return depositionResult.ToResult();

            var fileValidationResult = ValidateFiles(files);
            if (fileValidationResult.IsFailed)
                return fileValidationResult;

            var uploadedDocuments = new List<DocumentUserDeposition>();
            var deposition = depositionResult.Value;
            foreach (var file in files)
            {
                // TODO: Use depositionId for bucket folder
                var documentResult = await UploadDocumentFile(file, userResult.Value, $"{deposition.CaseId}/exhibits");
                if (documentResult.IsFailed)
                {
                    _logger.LogError(new Exception(documentResult.Errors.First().Message), "Unable to load one or more documents to storage");
                    _logger.LogInformation("Removing uploaded documents");
                    await DeleteUploadedFiles(uploadedDocuments.Select(d => d.Document).ToList());
                    return documentResult.ToResult();
                }

                uploadedDocuments.Add(new DocumentUserDeposition { Deposition = deposition, Document = documentResult.Value, User = userResult.Value });
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

        public async Task<Result<List<Document>>> GetExhibitsForUser(Guid depositionId, string identity)
        {
            var userResult = await _userService.GetUserByEmail(identity);
            if (userResult.IsFailed)
                return userResult.ToResult<List<Document>>();

            var depositionResult = await _depositionService.GetDepositionById(depositionId);
            if (depositionResult.IsFailed)
                return depositionResult.ToResult<List<Document>>();

            var documentUserDeposition = await _documentUserDepositionRepository.GetByFilter(x => x.DepositionId == depositionId && x.UserId == userResult.Value.Id, new[] { nameof(DocumentUserDeposition.Document) });

            return Result.Ok(documentUserDeposition.Select(d => d.Document).ToList());
        }

        public async Task<Result<string>> GetFileSignedUrl(string userEmail, Guid documentId)
        {
            var userResult = await _userService.GetUserByEmail(userEmail);
            if (userResult.IsFailed)
                return userResult.ToResult<string>();

            var documentUserDeposition = await _documentUserDepositionRepository.GetFirstOrDefaultByFilter(x => x.User == userResult.Value && x.DocumentId == documentId, new[] { nameof(DocumentUserDeposition.Document) });
            if (documentUserDeposition == null)
                return Result.Fail(new ResourceNotFoundError($"Could not find any document with Id {documentId} for user {userEmail}"));

            var expirationDate = DateTime.UtcNow.AddHours(_documentsConfiguration.PreSignedUrlValidHours);
            var signedUrl = _awsStorageService.GetFilePublicUri(documentUserDeposition.Document.FilePath, _documentsConfiguration.BucketName, expirationDate);

            return Result.Ok(signedUrl);
        }

        public Result<string> GetFileSignedUrl(Document document)
        {
            var expirationDate = DateTime.UtcNow.AddHours(_documentsConfiguration.PreSignedUrlValidHours);
            var signedUrl = _awsStorageService.GetFilePublicUri(document.FilePath,
                _documentsConfiguration.BucketName, expirationDate, document.DisplayName);

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

            var depositionResult = await _depositionService.GetDepositionById(documentUserDepositionResult.DepositionId);
            if (depositionResult.IsFailed)
                return depositionResult;

            if (depositionResult.Value.SharingDocumentId.HasValue)
                return Result.Fail(new ResourceConflictError("Can't share document while another document is being shared."));

            depositionResult.Value.SharingDocumentId = id;

            // Save ShareAt information as soon as the document is been shared
            var document = await _documentRepository.GetById(id);
            document.SharedAt = DateTime.UtcNow;
            await _documentRepository.Update(document);

            return await _depositionService.Update(depositionResult.Value);
        }

        public async Task<Result<Document>> GetDocument(Guid id)
        {
            var document = await _documentRepository.GetById(id, new[] { nameof(Document.AddedBy) });
            if (document == null)
                return Result.Fail(new ResourceNotFoundError("Document not found"));
            return Result.Ok(document);
        }

        public async Task<Result> UpdateDocument(DepositionDocument depositionDocument, string identity, FileTransferInfo file)
        {
            var userResult = await _userService.GetUserByEmail(identity);
            if (userResult.IsFailed)
                return userResult.ToResult();

            var depositionResult = await _depositionService.GetDepositionByIdWithDocumentUsers(depositionDocument.DepositionId);
            if (depositionResult.IsFailed)
                return depositionResult.ToResult();

            var fileValidationResult = ValidateFile(file);
            if (fileValidationResult.IsFailed)
                return fileValidationResult;

            var deposition = depositionResult.Value;

            // TODO: when original file was not a pdf we need to make sure we remove it from S3 since in that case it won't get overriden
            var fileName = $"{Path.GetFileNameWithoutExtension(depositionDocument.Document.Name)}.pdf";
            var documentResult = await UploadFileToStorage(file, userResult.Value, fileName, $"{deposition.CaseId}/{deposition.Id}/exhibits");
            if (documentResult.IsFailed)
            {
                _logger.LogError(new Exception(documentResult.Errors.First().Message), "Unable to update the document to storage");
                return documentResult;
            }

            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                try
                {
                    var documentUser = await _documentUserDepositionRepository.GetFirstOrDefaultByFilter(x => x.DocumentId == documentResult.Value.Id && x.DepositionId == deposition.Id);
                    await _documentUserDepositionRepository.Remove(documentUser);
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

        private async Task<Result<Document>> UploadFileToStorage(FileTransferInfo file, User user, string fileName, string parentPath)
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
            };

            return Result.Ok(document);
        }

        public async Task<Result<Document>> GetDocumentById(Guid documentId, string[] include = null)
        {
            var document = await _documentRepository.GetById(documentId, include);
            if (document == null)
                return Result.Fail(new ResourceNotFoundError($"Document with Id {documentId} "));

            return Result.Ok(document);
        }

        public async Task<Result<Document>> AddAnnotation(Guid depositionId, AnnotationEvent annotation)
        {
            var currentUser = await _userService.GetCurrentUserAsync();

            // TODO include sharingDocument
            var depositionResult = await _depositionService.GetDepositionById(depositionId);
            if (depositionResult.IsFailed)
                return depositionResult.ToResult<Document>();

            if (!depositionResult.Value.SharingDocumentId.HasValue)
                return Result.Fail(new ResourceNotFoundError($"There is no shared document for deposition {depositionId}"));

            var documentResult = await GetDocumentById(depositionResult.Value.SharingDocumentId.Value);
            if (documentResult.IsFailed)
                return documentResult;

            var document = documentResult.Value;
            if (document.AnnotationEvents == null)
                document.AnnotationEvents = new List<AnnotationEvent>();

            annotation.Author = currentUser;
            document.AnnotationEvents.Add(annotation);
            var updatedDocument = await _documentRepository.Update(document);

            return Result.Ok(updatedDocument);
        }

        public async Task<Result> RemoveDepositionUserDocuments(Guid documentId)
        {
            var documentUser = await _documentUserDepositionRepository.GetFirstOrDefaultByFilter(x => x.DocumentId == documentId);
            if (documentUser == null)
                return Result.Fail(new ResourceNotFoundError());

            await _documentUserDepositionRepository.Remove(documentUser);
            return Result.Ok();
        }
    }
}
