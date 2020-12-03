using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Commons;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IDepositionDocumentService
    {
        Task<Result<DepositionDocument>> UploadDocumentFile(KeyValuePair<string, FileTransferInfo> file, User user, string parentPath);
        Task DeleteUploadedFiles(List<DepositionDocument> uploadedDocuments);
    }
}