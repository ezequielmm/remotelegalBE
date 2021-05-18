using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using FluentResults;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using System;
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

        public AwsStorageService(ITransferUtility transferUtility, ILogger<AwsStorageService> logger)
        {
            _fileTransferUtility = transferUtility;
            _logger = logger;
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
    }
}
