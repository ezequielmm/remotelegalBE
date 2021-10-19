using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Helpers;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using PrecisionReporters.Platform.Shared.Helpers;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class AwsStorageService : IAwsStorageService
    {
        private readonly ITransferUtility _fileTransferUtility;
        private readonly ILogger<AwsStorageService> _logger;
        private readonly UrlPathConfiguration _urlPathConfiguration;
        private const string CUSTOM_METADATA_PREFIX = "x-amz-meta-";
        private readonly DocumentConfiguration _documentsConfiguration;

        public AwsStorageService(ITransferUtility transferUtility, ILogger<AwsStorageService> logger, IOptions<UrlPathConfiguration> urlPathConfiguration,
            IOptions<DocumentConfiguration> documentConfigurations)
        {
            _fileTransferUtility = transferUtility;
            _logger = logger;
            _urlPathConfiguration = urlPathConfiguration.Value;
            _documentsConfiguration = documentConfigurations.Value ?? throw new ArgumentException(nameof(documentConfigurations));
        }

        public async Task<Result> UploadMultipartAsync(string keyName, FileTransferInfo file, string bucketName)
        {
            _logger.LogInformation("UploadMultipartAsync from file:{file} size: {$size} on bucket {2} path string {$keyName}", file.Name, file.Length, bucketName, keyName);
            await _fileTransferUtility.S3Client.UploadObjectFromStreamAsync(bucketName, keyName, file.FileStream, null);
            return Result.Ok();
        }

        public async Task<Result> UploadObjectFromStreamAsync(string keyName, Stream fileStream, string bucketName)
        {
            try
            {
                await _fileTransferUtility.S3Client.UploadObjectFromStreamAsync(bucketName, keyName, fileStream, null);
            }
            catch (Exception ex)
            {
                return Result.Fail(new ExceptionalError("Error saving file form stream.", ex));
            }

            return Result.Ok();
        }

        public async Task<Result> UploadObjectFromFileAsync(string fileName, string documentKeyName, string bucketName)
        {
            try
            {
                using (var file = File.OpenRead(fileName))
                {
                    var uploadedDocument = await UploadObjectFromStreamAsync(documentKeyName, file, bucketName);
                    if (uploadedDocument.IsFailed)
                        return uploadedDocument;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return Result.Fail(new Error(ex.Message));
            }

            return Result.Ok();
        }

        public async Task<Result> DeleteObjectAsync(string bucketName, string key)
        {
            var response = await _fileTransferUtility.S3Client.DeleteObjectAsync(bucketName, key);
            if (response.HttpStatusCode != HttpStatusCode.OK)
                return Result.Fail($"Unable to delete file {key} from bucket {bucketName}");
            return Result.Ok();
        }

        public string GetFilePublicUri(string key, string bucketName, DateTime expirationDate, string displayName = null, bool inline = false)
        {
            string contentDisposition;
            displayName = displayName.Replace(",", ""); //removing comma character as there's an error from AWS when using it on a URL.

            if (inline)
                contentDisposition = "inline";
            else
            {
                contentDisposition = string.IsNullOrWhiteSpace(displayName) ? "attachment" : $"attachment;filename={displayName}";
            }

            AWSConfigsS3.UseSignatureVersion4 = bool.Parse(_documentsConfiguration.UseSignatureVersion4);

            var signedUrl = _fileTransferUtility.S3Client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Expires = expirationDate,
                ResponseHeaderOverrides = {
                    ContentDisposition = contentDisposition
                }
            }
            );

            var uriBuilder = new UriBuilder(signedUrl);
            var queryString = HttpUtility.ParseQueryString(uriBuilder.Query);

            uriBuilder.Query = queryString.ToString().Replace("%2c", ""); //removing comma character as there's an error from AWS when using it on a URL.
            return uriBuilder.Uri.ToString();
        }

        public async Task<Stream> GetObjectAsync(string objectKey, string bucketName)
        {
            return await _fileTransferUtility.S3Client.GetObjectStreamAsync(bucketName,objectKey, null);
        }

        
        public async Task<List<S3Object>> GetAllObjectInBucketAsync(string bucket)
        {
            var result = await _fileTransferUtility.S3Client.ListObjectsAsync(bucket);
            return result.S3Objects;
        }

        public string GetCannedPrivateURL(string key, DateTime expirationDate, string privateKeyId, string xmlKey, string policyStatement)
        {
            var filePublicUri = $"{_urlPathConfiguration.FrontendBaseUrl}{key}";
            if (Debugger.IsAttached)
            {
                return $"{_urlPathConfiguration.FrontendBaseUrl}/{_documentsConfiguration.BucketName}/{filePublicUri}";
            }
            var duration = (expirationDate - DateTime.UtcNow);
            var durationNumber = (int)duration.TotalSeconds;

            var uriSignature = StorageHelper.CreateCannedPrivateURL(filePublicUri, "seconds", durationNumber.ToString(), privateKeyId, xmlKey, policyStatement);
            return uriSignature;
        }

        public async Task<Result<FileTransferInfo>> UploadAsync(string keyName, string pathFile, string bucketName)
        {
            _logger.LogInformation("UploadAsync for path file :{pathFile} in bucket name: {2} uploaded Url: {3}", pathFile, bucketName, keyName);
            await _fileTransferUtility.S3Client.UploadObjectFromFilePathAsync(bucketName, keyName, pathFile, null);

            return Result.Ok();
        }

        /// <summary>
        /// Generate a presigned URL that can be used to access the file named
        /// in the ojbectKey parameter for the amount of time specified in the
        /// duration parameter.
        /// </summary>
        /// <param name="key">The name of the S3 bucket containing the object for which to create the presigned URL</param>
        /// <param name="bucketName">The name of the object to access with the presigned URL.</param>
        /// <param name="expirationTime">The length of time for which the presigned URL will be valid.</param>
        /// <param name="metadata">collection of metadata to append to Presigned URL</param>
        /// <returns>A string representing the generated presigned URL</returns>
        public Result<PreSignedUrlDto> GetPreSignedPutUrl(string key, string bucketName, DateTime expirationTime, Dictionary<string, string> metadata = null)
        {
            PreSignedUrlDto presigned;
            try
            {
                GetPreSignedUrlRequest request = new GetPreSignedUrlRequest()
                {
                    BucketName = bucketName,
                    Key = key,
                    Verb = HttpVerb.PUT,
                    Expires = expirationTime,
                    Protocol = Debugger.IsAttached? Protocol.HTTP : Protocol.HTTPS
                };
                
                presigned = new PreSignedUrlDto();
                if (metadata != null)
                {
                    foreach (var data in metadata)
                    {
                        request.Metadata.Add(data.Key, data.Value.ToString());
                    }
                    presigned.Headers = metadata;
                }
                presigned.Url = _fileTransferUtility.S3Client.GetPreSignedURL(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("An AmazonS3Exception was thrown: {0}", ex.Message);
                return Result.Fail(new ExceptionalError($"Error generating PreSignedPutUrl: ", ex));
            }

            return Result.Ok(presigned);
        }
    }
}
