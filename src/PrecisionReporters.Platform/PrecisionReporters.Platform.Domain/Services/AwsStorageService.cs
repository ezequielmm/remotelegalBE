using Amazon.Runtime;
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
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class AwsStorageService : IAwsStorageService
    {
        private readonly ITransferUtility _fileTransferUtility;
        private readonly ILogger<AwsStorageService> _logger;
        private readonly UrlPathConfiguration _urlPathConfiguration;
        private const string CUSTOM_METADATA_PREFIX = "x-amz-meta-";

        public AwsStorageService(ITransferUtility transferUtility, ILogger<AwsStorageService> logger, IOptions<UrlPathConfiguration> urlPathConfiguration)
        {
            _fileTransferUtility = transferUtility;
            _logger = logger;
            _urlPathConfiguration = urlPathConfiguration.Value;
        }
        public async Task<Result<FileTransferInfo>> UploadMultipartAsync(string keyName, string pathFile, string bucketName)
        {
            var file = new FileTransferInfo();
            using var stream = File.OpenRead(pathFile);
            file.FileStream = stream;
            file.Length = stream.Length;

            var result = await UploadMultipartAsync(keyName, file, bucketName);

            if(result.IsFailed)
                return result;

            return Result.Ok(file);
        }

        public async Task<Result> UploadMultipartAsync(string keyName, FileTransferInfo file, string bucketName)
        {
            // Create list to store upload part responses.
            var uploadResponses = new List<UploadPartResponse>();

            // Setup information required to initiate the multipart upload.
            var initiateRequest = new InitiateMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = keyName
            };

            // Initiate the upload.
            var initResponse =
                await _fileTransferUtility.S3Client.InitiateMultipartUploadAsync(initiateRequest);

            // Upload parts
            var partSize = 5 * (long)Math.Pow(2, 20); // 5 MB

            try
            {
                _logger.LogDebug("Uploading parts");

                long filePosition = 0;
                for (int i = 1; filePosition < file.Length; i++)
                {
                    var uploadRequest = new UploadPartRequest
                    {
                        BucketName = bucketName,
                        Key = keyName,
                        UploadId = initResponse.UploadId,
                        PartNumber = i,
                        PartSize = partSize,
                        InputStream = file.FileStream
                    };

                    // Track upload progress.
                    uploadRequest.StreamTransferProgress +=
                        new EventHandler<StreamTransferProgressArgs>(UploadPartProgressEventCallback);

                    // Upload a part and add the response to our list.
                    uploadResponses.Add(await _fileTransferUtility.S3Client.UploadPartAsync(uploadRequest));

                    filePosition += partSize;
                }

                // Setup to complete the upload.
                var completeRequest = new CompleteMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = keyName,
                    UploadId = initResponse.UploadId
                };
                completeRequest.AddPartETags(uploadResponses);

                // Complete the upload.
                await _fileTransferUtility.S3Client.CompleteMultipartUploadAsync(completeRequest);

                _logger.LogDebug($"File uploaded {keyName}");

                return Result.Ok();
            }
            catch (Exception exception)
            {
                _logger.LogError("An AmazonS3Exception was thrown: {0}", exception.Message);

                // Abort the upload.
                var abortMPURequest = new AbortMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = keyName,
                    UploadId = initResponse.UploadId
                };
                await _fileTransferUtility.S3Client.AbortMultipartUploadAsync(abortMPURequest);


                return Result.Fail(new ExceptionalError($"Error loading file {keyName}", exception));
            }
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
            if (inline)
                contentDisposition = "inline";
            else
            {
                //HttpUtility.UrlEncode change the white space for a + character, with this line the URL is encoding the special characters and replace the + character for white space again
                //This is necessary for getting the file name properly and avoid errors whenever the SignedURL is called
                contentDisposition = string.IsNullOrWhiteSpace(displayName) ? "attachment" : $"attachment;filename={HttpUtility.UrlEncode(displayName).Replace('+', ' ')}";
            }
            return _fileTransferUtility.S3Client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Expires = expirationDate,
                ResponseHeaderOverrides = {
                    ContentDisposition = contentDisposition
                }
            }
            );
        }

        public async Task<Stream> GetObjectAsync(string objectKey, string bucketName)
        {
            var request = new GetObjectRequest();
            request.BucketName = bucketName;
            request.Key = objectKey;
            var response = await _fileTransferUtility.S3Client.GetObjectAsync(request);
            return response.ResponseStream;
        }

        private void UploadPartProgressEventCallback(object sender, StreamTransferProgressArgs e)
        {
            // Process event. 
            _logger.LogDebug("{0}/{1}", e.TransferredBytes, e.TotalBytes);
        }

        public async Task<List<S3Object>> GetAllObjectInBucketAsync(string bucket)
        {
            var result = await _fileTransferUtility.S3Client.ListObjectsAsync(bucket);
            return result.S3Objects;
        }

        public string GetCannedPrivateURL(string key, DateTime expirationDate, string privateKeyId, string xmlKey, string policyStatement)
        {
            var filePublicUri = $"{_urlPathConfiguration.FrontendBaseUrl}{key}";
            var duration = (expirationDate - DateTime.UtcNow);
            var durationNumber = (int)duration.TotalSeconds;

            var uriSignature = StorageHelper.CreateCannedPrivateURL(filePublicUri, "seconds", durationNumber.ToString(), privateKeyId, xmlKey, policyStatement);
            return uriSignature;
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
        public Result<PreSignedUrlDto> GetPreSignedPutUrl(string key, string bucketName, DateTime expirationTime, Dictionary<string, object> metadata = null)
        {
            PreSignedUrlDto presigned;
            try
            {
                GetPreSignedUrlRequest request = new GetPreSignedUrlRequest()
                {
                    BucketName = bucketName,
                    Key = key,
                    Verb = HttpVerb.PUT,
                    Expires = expirationTime
                };
                
                presigned = new PreSignedUrlDto();
                if (metadata != null)
                {
                    presigned.Headers = new Dictionary<string, string>();
                    foreach (var data in metadata)
                    {
                        var customKey = CUSTOM_METADATA_PREFIX + data.Key;
                        customKey = customKey.ToHypenCase();
                        request.Metadata.Add(data.Key.ToHypenCase(), data.Value.ToString());
                        presigned.Headers.Add(customKey, data.Value.ToString());
                    }
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
