using Amazon.S3.Model;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Commons;
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
    }
}