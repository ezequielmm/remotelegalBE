﻿using Amazon.S3.Model;
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
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
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

        public DocumentService(IAwsStorageService awsStorageService, IOptions<DocumentConfiguration> documentConfigurations, ILogger<DocumentService> logger,
            IUserService userService, IDocumentUserDepositionRepository documentUserDepositionRepository,
            IPermissionService permissionService, ITransactionHandler transactionHandler, IDocumentRepository documentRepository, IDepositionDocumentRepository depositionDocumentRepository, IDepositionRepository depositionRepository)
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

        public async Task<Result<Document>> UploadDocumentFile(FileTransferInfo file, User user, string parentPath, DocumentType documentType)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";

            var document = await UploadFileToStorage(file, user, fileName, parentPath, documentType);
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

        private Result ValidateTranscriptions(List<FileTransferInfo> files)
        {
            if (files.Any(f => f.Length > _documentsConfiguration.MaxFileSize))
                return Result.Fail(new InvalidInputError("Exhibit size exceeds the allowed limit"));

            if (files.Any(f => !_documentsConfiguration.AcceptedTranscriptionExtensions.Contains(Path.GetExtension(f.Name))))
                return Result.Fail(new InvalidInputError("Extension of the file is not correct"));

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
                var documentResult = await UploadDocumentFile(file, userResult.Value, $"{depositionResult.CaseId}/{depositionResult.Id}/{folder}", documentType);
                if (documentResult.IsFailed)
                {
                    _logger.LogError(new Exception(documentResult.Errors.First().Message), "Unable to load one or more documents to storage");
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
            var transactionResult = await _transactionHandler.RunAsync(async () =>
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

        public async Task<Result<List<string>>> GetFileSignedUrl(Guid depositionId, DocumentIdListDto documentIdListDto)
        {
            var signedUrlList = new List<string>();
            Expression<Func<DepositionDocument, bool>> filter = x => x.DepositionId == depositionId && documentIdListDto.DocumentsId.Contains(x.DocumentId);

            var depositionDocument = await _depositionDocumentRepository.GetByFilter(filter, new[] { nameof(DepositionDocument.Document) });
            if (depositionDocument == null)
                return Result.Fail(new ResourceNotFoundError($"Could not find any document for deposition {depositionId}"));

            foreach (var dd in depositionDocument)
            {
                var signedUrl = GetFileSignedUrl(dd.Document);
                signedUrlList.Add(signedUrl.Value);
            }
            
            return Result.Ok(signedUrlList);
        }

        public async Task<Result<string>> GetFileSignedUrl(Guid depositionId, Guid documentId)
        {
            var depositionDocument = await _depositionDocumentRepository.GetFirstOrDefaultByFilter(x => x.DepositionId == depositionId && x.DocumentId == documentId, new[] { nameof(DepositionDocument.Document) });
            if (depositionDocument == null)
                return Result.Fail(new ResourceNotFoundError($"Could not find any document with Id {documentId}"));

            var signedUrl = GetFileSignedUrl(depositionDocument.Document);

            return Result.Ok(signedUrl.Value);
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

        public async Task<Result> UpdateDocument(DepositionDocument depositionDocument, string identity, FileTransferInfo file, string folder, DocumentType documentType)
        {
            var userResult = await _userService.GetUserByEmail(identity);
            if (userResult.IsFailed)
                return userResult.ToResult();

            var include = new[] { nameof(Deposition.DocumentUserDepositions) };
            var depositionResult = await _depositionRepository.GetById(depositionDocument.DepositionId, include);
            if (depositionResult == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {depositionDocument.DepositionId} not found."));

            var fileValidationResult = ValidateFile(file);
            if (fileValidationResult.IsFailed)
                return fileValidationResult;

            var deposition = depositionResult;

            // TODO: when original file was not a pdf we need to make sure we remove it from S3 since in that case it won't get overriden
            var fileName = $"{Path.GetFileNameWithoutExtension(depositionDocument.Document.Name)}.pdf";
            var filePath = $"{deposition.CaseId}/{deposition.Id}/{folder}";
            var documentResult = await UploadFileToStorage(file, userResult.Value, fileName, filePath, documentType);
            if (documentResult.IsFailed)
            {
                _logger.LogError(new Exception(documentResult.Errors.First().Message), "Unable to update the document to storage");
                return documentResult;
            }

            var transactionResult = await _transactionHandler.RunAsync(async () =>
            {
                try
                {
                    // Update document file Name and Path with the PDF extension that PDFTron response
                    var updateDocument = await _documentRepository.GetById(depositionDocument.DocumentId);
                    updateDocument.Name = fileName;
                    updateDocument.DisplayName = $"{Path.GetFileNameWithoutExtension(depositionDocument.Document.DisplayName)}.pdf";
                    updateDocument.FilePath = $"/{filePath}/{fileName}";
                    updateDocument.Type = ".pdf";
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
            var includes = new[] { nameof(Deposition.Requester), nameof(Deposition.Participants),
                nameof(Deposition.Case), nameof(Deposition.AddedBy),nameof(Deposition.Caption)};
            var depositionResult = await _depositionRepository.GetById(depositionId, includes);
            if (depositionResult == null)
                return Result.Fail(new ResourceNotFoundError($"Deposition with id {depositionId} not found."));

            if (!depositionResult.SharingDocumentId.HasValue)
                return Result.Fail(new ResourceNotFoundError($"There is no shared document for deposition {depositionId}"));

            var documentResult = await GetDocumentById(depositionResult.SharingDocumentId.Value);
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

        public async Task<Result> ShareEnteredExhibit(Guid depositionId, Guid documentId)
        {
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

            depositionResult.SharingDocumentId = document.Id;
            document.SharedAt = DateTime.UtcNow;
            await _documentRepository.Update(document);

            return Result.Ok(await _depositionRepository.Update(depositionResult));
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
                    await _documentRepository.Remove(document);
                    await _documentUserDepositionRepository.Remove(documentUser);
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
    }
}
