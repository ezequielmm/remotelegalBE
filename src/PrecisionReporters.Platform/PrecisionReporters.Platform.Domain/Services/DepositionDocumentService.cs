using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class DepositionDocumentService : IDepositionDocumentService
    {
        private readonly IAwsStorageService _awsStorageService;
        private readonly ILogger<DepositionDocumentService> _logger;
        private readonly DepositionDocumentConfiguration _depositionsDocumentsConfiguration;

        public DepositionDocumentService(IAwsStorageService awsStorageService, IOptions<DepositionDocumentConfiguration> depositionDocumentConfigurations, ILogger<DepositionDocumentService> logger)
        {
            _awsStorageService = awsStorageService;
            _depositionsDocumentsConfiguration = depositionDocumentConfigurations.Value ?? throw new ArgumentException(nameof(depositionDocumentConfigurations));
            _logger = logger;
        }

        public async Task<Result<DepositionDocument>> UploadDocumentFile(KeyValuePair<string, FileTransferInfo> file, User user, string parentPath)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Value.Name)}";
            var documentKeyName = $"/{parentPath}/{fileName}";

            var uploadedDocument =
                await _awsStorageService.UploadMultipartAsync(documentKeyName, file.Value, _depositionsDocumentsConfiguration.BucketName);
            if (uploadedDocument.IsFailed)
                return Result.Fail(new Error($"Unable to upload document {file.Value.Name}. {uploadedDocument.Errors[0]}"));

            var document = new DepositionDocument
            {
                Type = Path.GetExtension(file.Value.Name),
                AddedBy = user,
                Name = fileName,
                FilePath = documentKeyName,
                FileKey = file.Key
            };
            return Result.Ok(document);
        }

        public async Task DeleteUploadedFiles(List<DepositionDocument> uploadedDocuments)
        {
            foreach (var document in uploadedDocuments)
            {
                var deleteObjectResponse = await _awsStorageService.DeleteObjectAsync(_depositionsDocumentsConfiguration.BucketName, document.FilePath);
                if (deleteObjectResponse.IsFailed)
                    _logger.LogError($"Error while trying to delete document {document.Name} from {document.FilePath}. Storage response was {deleteObjectResponse.Errors[0]}");
            }
        }
    }
}
