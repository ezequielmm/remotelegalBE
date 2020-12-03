using System.Threading.Tasks;
using Amazon.S3.Model;
using FluentResults;
using PrecisionReporters.Platform.Domain.Commons;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IAwsStorageService
    {
        Task<Result> UploadMultipartAsync(string keyName, FileTransferInfo file, string bucketName);
        Task<Result> DeleteObjectAsync(string bucketName, string key);
    }
}