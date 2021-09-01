using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Util;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3.Model;
using UploadExhibitLambda;
using UploadExhibitLambda.Wrappers.Interface;
using Xunit;
using static PrecisionReporters.Platform.Shared.Commons.ApplicationConstants;

namespace PrecisionReporters.Platform.UnitTests.Lambdas
{
    public class UploadExhibitFunctionTests
    {
        [Fact]
        public async Task UploadExhibit_ShouldSkipUploadAndSendNotification_WhenNullRecords()
        {
            // Arrange
            var s3Client = new Mock<IAmazonS3>();
            var snsClient = new Mock<IAmazonSimpleNotificationService>();
            var secretManagerClient = new Mock<IAmazonSecretsManager>();
            var metadataWrapper = new Mock<IMetadataWrapper>();
            snsClient.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PublishResponse { MessageId = "1", HttpStatusCode = System.Net.HttpStatusCode.OK });
            var lambdaContext = new Mock<ILambdaContext>();
            lambdaContext.Setup(x => x.Logger)
                .Returns(new Mock<ILambdaLogger>().Object);
            var ev = new S3Event();
            var function = new UploadExhibitFunction(s3Client.Object, snsClient.Object, secretManagerClient.Object, metadataWrapper.Object);

            // Act
            var res = await function.UploadExhibit(ev, lambdaContext.Object);

            // Assert
            Assert.False(res);
            snsClient.Verify(x => x.PublishAsync(It.Is<PublishRequest>(c => c.Message.Contains(UploadExhibitsNotificationTypes.InvalidS3Structure)), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadExhibit_ShouldSkipUploadAndSendNotification_WhenGetSecretFails()
        {
            // Arrange
            var s3Client = new Mock<IAmazonS3>();
            var snsClient = new Mock<IAmazonSimpleNotificationService>();
            var secretManagerClient = new Mock<IAmazonSecretsManager>();
            var metadataWrapper = new Mock<IMetadataWrapper>();
            metadataWrapper.Setup(mock => mock.GetMetadataByKey(It.IsAny<GetObjectMetadataResponse>(), It.IsAny<string>()))
                .Returns(Guid.NewGuid().ToString);
            snsClient.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PublishResponse { MessageId = "1", HttpStatusCode = System.Net.HttpStatusCode.OK });
            var lambdaContext = new Mock<ILambdaContext>();
            lambdaContext.Setup(x => x.Logger)
                .Returns(new Mock<ILambdaLogger>().Object);
            var ev = CreateS3Event();

            secretManagerClient.Setup(mock => mock.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(),
                It.IsAny<CancellationToken>()))
                .Throws(new Exception());
            var function = new UploadExhibitFunction(s3Client.Object, snsClient.Object, secretManagerClient.Object, metadataWrapper.Object);

            // Act
            var result = await function.UploadExhibit(ev, lambdaContext.Object);

            // Assert
            Assert.False(result);
            snsClient.Verify(x => x.PublishAsync(It.Is<PublishRequest>(c => c.Message.Contains(UploadExhibitsNotificationTypes.ExceptionInLambda)), It.IsAny<CancellationToken>()), Times.Once);
        }

        private S3Event CreateS3Event()
        {
            return new S3Event
            {
                Records = new List<S3EventNotification.S3EventNotificationRecord>
                {
                    new S3EventNotification.S3EventNotificationRecord()
                    {
                        S3 = new S3EventNotification.S3Entity()
                        {
                            Bucket = new S3EventNotification.S3BucketEntity()
                            {
                                Arn = "mock.arn.bucket",
                                Name = "bucket-mock",
                                OwnerIdentity = new S3EventNotification.UserIdentityEntity()
                                {
                                    PrincipalId = Guid.NewGuid().ToString()
                                }
                            },
                            Object = new S3EventNotification.S3ObjectEntity()
                            {
                                ETag = "tag 1 mock",
                                Key = "mock/file.pdf",
                                Sequencer = Guid.NewGuid().ToString(),
                                Size = 1024 * 5,
                                VersionId = "currentversion"
                            }
                        }
                    }
                }
            };
        }
    }
}
