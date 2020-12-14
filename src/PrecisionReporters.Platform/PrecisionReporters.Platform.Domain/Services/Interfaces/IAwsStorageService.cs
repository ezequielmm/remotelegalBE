using FluentResults;
using PrecisionReporters.Platform.Domain.Commons;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IAwsStorageService
    {
        Task<Result> UploadMultipartAsync(string keyName, FileTransferInfo file, string bucketName);
        Task<Result> DeleteObjectAsync(string bucketName, string key);
    }
}