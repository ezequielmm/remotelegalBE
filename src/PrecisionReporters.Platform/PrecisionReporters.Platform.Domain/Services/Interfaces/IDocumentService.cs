using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Commons;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<Result<Document>> UploadDocumentFile(KeyValuePair<string, FileTransferInfo> file, User user, string parentPath);
        Task<Result<Document>> UploadDocumentFile(FileTransferInfo file, User user, string parentPath);
        Task DeleteUploadedFiles(List<Document> uploadedDocuments);
        Result ValidateFiles(List<FileTransferInfo> files);
        Task<Result> UploadDocuments(Guid id, string identity, List<FileTransferInfo> files);
    }
}