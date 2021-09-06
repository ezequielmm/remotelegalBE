using Amazon.S3.Model;
using FluentResults;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Shared.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IAwsStorageService
    {
        Task<Result> UploadMultipartAsync(string keyName, FileTransferInfo file, string bucketName);
        Task<Result> DeleteObjectAsync(string bucketName, string key);
        string GetFilePublicUri(string key, string bucketName, DateTime expirationDate, string displayName = null, bool inline = false);
        Task<Stream> GetObjectAsync(string objectKey, string bucketName);
        Task<Result> UploadObjectFromStreamAsync(string keyName, Stream fileStream, string bucketName);
        Task<List<S3Object>> GetAllObjectInBucketAsync(string bucket);
        Task<Result> UploadObjectFromFileAsync(string fileName, string documentKeyName, string bucketName);
        string GetCannedPrivateURL(string key, DateTime expirationDate, string privateKeyId, string xmlKey, string policyStatement);
        Task<Result<FileTransferInfo>> UploadAsync(string keyName, string pathFile, string bucketName);
        Result<PreSignedUrlDto> GetPreSignedPutUrl(string key, string bucketName, DateTime expirationTime, Dictionary<string, object> metadata = null);

    }
}