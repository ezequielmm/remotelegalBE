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
        Result ValidateFile(FileTransferInfo file);
        Task<Result> UploadDocuments(Guid id, string identity, List<FileTransferInfo> files);
        Task<Result> UpdateDocument(DepositionDocument depositionDocument, string identity, FileTransferInfo file);
        Task<Result<List<Document>>> GetExhibitsForUser(Guid depositionId, string identity);
        Task<Result<string>> GetFileSignedUrl(string userEmail, Guid documentId);
        Task<Result<Document>> GetDocumentById(Guid documentId, string[] include = null);
        Task<Result<Document>> AddAnnotation(Guid depositionId, AnnotationEvent annotation);
        Task<Result> Share(Guid id, string userEmail);
        Task<Result<Document>> GetDocument(Guid id);
        Result<string> GetFileSignedUrl(Document document);
        Task<Result> RemoveDepositionUserDocuments(Guid documentId);
    }
}