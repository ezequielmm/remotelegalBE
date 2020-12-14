using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using FluentResults;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

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

        public async Task<Result> DeleteObjectAsync(string bucketName, string key)
        {
            var response = await _fileTransferUtility.S3Client.DeleteObjectAsync(bucketName, key);
            if (response.HttpStatusCode != HttpStatusCode.OK)
                return Result.Fail($"Unable to delete file {key} from bucket {bucketName}");
            return Result.Ok();
        }

        private void UploadPartProgressEventCallback(object sender, StreamTransferProgressArgs e)
        {
            // Process event. 
            _logger.LogDebug("{0}/{1}", e.TransferredBytes, e.TotalBytes);
        }
    }
}
