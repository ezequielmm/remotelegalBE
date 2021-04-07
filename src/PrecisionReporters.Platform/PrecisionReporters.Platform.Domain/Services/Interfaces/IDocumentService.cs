﻿using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<Result<Document>> UploadDocumentFile(KeyValuePair<string, FileTransferInfo> file, User user, string parentPath, DocumentType documentType);
        Task<Result<Document>> UploadDocumentFile(FileTransferInfo file, User user, string parentPath, DocumentType documentType);
        Task DeleteUploadedFiles(List<Document> uploadedDocuments);
        Result ValidateFiles(List<FileTransferInfo> files);
        Result ValidateFile(FileTransferInfo file);
        Task<Result> UploadDocuments(Guid id, string identity, List<FileTransferInfo> files, string folder, DocumentType documentType);
        Task<Result> UpdateDocument(Document document, DepositionDocument depositionDocument, string identity, string temproralPath);
        Task<Result<List<Document>>> GetExhibitsForUser(Guid depositionId, string identity);
        Task<Result<string>> GetFileSignedUrl(Guid documentId);
        Task<Result<string>> GetFileSignedUrl(Guid depositionId, Guid documentId);
        Task<Result<Document>> GetDocumentById(Guid documentId, string[] include = null);
        Task<Result<Document>> AddAnnotation(Guid depositionId, AnnotationEvent annotation);
        Task<Result> Share(Guid id, string userEmail);
        Task<Result> ShareEnteredExhibit(Guid depositionId, Guid documentId);
        Task<Result<Document>> GetDocument(Guid id);
        Result<string> GetFileSignedUrl(Document document);
        Task<Result> RemoveDepositionUserDocuments(Guid documentId);
        Task<Result> UploadTranscriptions(Guid id, List<FileTransferInfo> files);
        Task<Result> RemoveDepositionDocument(Guid depositionId, Guid documentId);
        Task<Result<List<string>>> GetFileSignedUrl(Guid depositionId, List<Guid> documentIds);
    }
}