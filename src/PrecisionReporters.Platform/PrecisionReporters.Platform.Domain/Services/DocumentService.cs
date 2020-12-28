using FluentResults;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
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
        private readonly ILogger<DocumentService> _logger;
        private readonly DocumentConfiguration _documentsConfiguration;

        public DocumentService(IAwsStorageService awsStorageService, IOptions<DocumentConfiguration> documentConfigurations, ILogger<DocumentService> logger, IUserService userService, IDepositionService depositionService, IDocumentUserDepositionRepository documentUserDepositionRepository)
        {
            _awsStorageService = awsStorageService;
            _documentsConfiguration = documentConfigurations.Value ?? throw new ArgumentException(nameof(documentConfigurations));
            _logger = logger;
            _userService = userService;
            _depositionService = depositionService;
            _documentUserDepositionRepository = documentUserDepositionRepository;
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

            try
            {
                await _documentUserDepositionRepository.CreateRange(uploadedDocuments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to add documents to deposition");
                await DeleteUploadedFiles(uploadedDocuments.Select(d => d.Document).ToList());
                return Result.Fail(new ExceptionalError("Unable to add documents to deposition", ex));
            }

            return Result.Ok();
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
    }
}
