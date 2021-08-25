using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.SecretsManager;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static PrecisionReporters.Platform.Shared.Commons.ApplicationConstants;

namespace UploadExhibitLambda.Tests
{
    public class UploadExhibitFunctionTests
    {
        [Fact]
        public async Task UploadExhibit_ShouldSkipUploadAndSendNotification_WhenNullRecords()
        {
            var s3Client = new Mock<IAmazonS3>();
            var snsClient = new Mock<IAmazonSimpleNotificationService>();
            var secretManagerClient = new Mock<IAmazonSecretsManager>();
            snsClient.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new PublishResponse { MessageId = "1", HttpStatusCode = System.Net.HttpStatusCode.OK });
            var lambdaContext = new Mock<ILambdaContext>();
            lambdaContext.Setup(x => x.Logger)
                         .Returns(new Mock<ILambdaLogger>().Object);
            var ev = new S3Event();
            var function = new UploadExhibitFunction(s3Client.Object, snsClient.Object, secretManagerClient.Object);

            var res = await function.UploadExhibit(ev, lambdaContext.Object);

            Assert.False(res);
            snsClient.Verify(x => x.PublishAsync(It.Is<PublishRequest>(c => c.Message.Contains(UploadExhibitsNotificationTypes.InvalidS3Structure)), It.IsAny<CancellationToken>()), Times.Once);
        }

        //TODO: Added first test as an example, we should continue testing the critical path
    }
}
