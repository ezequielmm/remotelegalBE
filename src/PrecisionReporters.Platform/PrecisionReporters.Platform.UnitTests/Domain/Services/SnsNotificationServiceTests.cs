using Microsoft.Extensions.Logging;
using Moq;
using PrecisionReporters.Platform.Domain.Handlers.Notifications.Interfaces;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Wrappers.Interfaces;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class SnsNotificationServiceTests
    {
        private readonly SnsNotificationService _service;
        private readonly Mock<ILogger<SnsNotificationService>> _logger;
        private readonly Mock<IAwsSnsWrapper> _awsSnsWrapper;
        private readonly Mock<ILambdaExceptionHandler> _lambdaExceptionHandler;
        private readonly Mock<IExhibitNotificationHandler> _exhibitNotificationHandler;
        private readonly Mock<ISubscriptionMessageHandler> _subscriptionMessageHandler;
        private readonly Mock<IMessageSignatureHandler> _messageSignatureHandler;
        private readonly Mock<IUnknownMessageHandler> _unknownMessageHandler;

        public SnsNotificationServiceTests()
        {
            _logger = new Mock<ILogger<SnsNotificationService>>();
            _awsSnsWrapper = new Mock<IAwsSnsWrapper>();
            _lambdaExceptionHandler = new Mock<ILambdaExceptionHandler>();
            _exhibitNotificationHandler = new Mock<IExhibitNotificationHandler>();
            _subscriptionMessageHandler = new Mock<ISubscriptionMessageHandler>();
            _messageSignatureHandler = new Mock<IMessageSignatureHandler>();
            _unknownMessageHandler = new Mock<IUnknownMessageHandler>();

            _service = new SnsNotificationService(_logger.Object, _awsSnsWrapper.Object, _lambdaExceptionHandler.Object, _exhibitNotificationHandler.Object, _subscriptionMessageHandler.Object, _messageSignatureHandler.Object, _unknownMessageHandler.Object);
        }

        [Fact]
        public async Task SendRawEmailNotification_ReturnOk()
        {
            // Arrange
            var messageId = "8efc90fa-022d-564c-b5ec-c959054f744d";
            var messageText = "{  \"Type\" : \"Notification\",  \"MessageId\" : \"8efc90fa-022d-564c-b5ec-c959054f744d\",  \"TopicArn\" : \"arn:aws:sns:us-east-1:747865543072:notifications-dev\",  \"Message\" : \"{\\\"NotificationType\\\":\\\"ExhibitUploaded\\\",\\\"Context\\\":{\\\"Name\\\":\\\"15da216f-c766-4a02-bf73-f4904655c730.pdf\\\",\\\"DisplayName\\\":\\\"test.pdf\\\",\\\"FilePath\\\":\\\"files/0bf0b97c-890f-4713-982c-08d957836a06/a112dd26-e5bf-4343-90e8-188da7d3e016/Exhibit/15da216f-c766-4a02-bf73-f4904655c730.pdf\\\",\\\"Size\\\":185109,\\\"AddedBy\\\":\\\"96ec144d-ed54-4bdb-95b4-08d94ae4acad\\\",\\\"DocumentType\\\":\\\"Exhibit\\\",\\\"Type\\\":\\\".pdf\\\",\\\"DepositionId\\\":\\\"a112dd26-e5bf-4343-90e8-188da7d3e016\\\"}}\",  \"Timestamp\" : \"2021-08-27T15:30:42.611Z\",  \"SignatureVersion\" : \"1\",  \"Signature\" : \"l4W6BHhNrGIhCrzo4Vpe/+WAMj65PzCmOfWPesjuLocGFuz5WAlcbve895MzWXuqlOwKTPgOLq375DapON24JT01sAZ1rjdHqxSfyJ5naabPFYLoB4Vn8ibcrBD5Wdt8B11C7AowntNl4BQS2MFjV+OOzGEJ4WgRxVocHUMd+/CUIddx1m57MV8Pl/8wQJljmXfKgFHAdX95MrTygZwdLnSYI6v+NLd8VSX7obcbCX4WuDEDmp26/MGUmQZnopzD4U5nHV1kWd3QzlWmIlezovZej7xdl7Dql8sSJ/HzqfayPPMxilTC7DnK19YCi6UDCDHp01h1FfabZKJRHX4hHA==\",  \"SigningCertURL\" : \"https://sns.us-east-1.amazonaws.com/SimpleNotificationService-010a507c1833636cd94bdb98bd93083a.pem\",  \"UnsubscribeURL\" : \"https://sns.us-east-1.amazonaws.com/?Action=Unsubscribe&SubscriptionArn=arn:aws:sns:us-east-1:747865543072:notifications-dev:3f53d41e-597b-4d59-a129-e7050e3f3238\"}";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(messageText));
            _awsSnsWrapper.Setup(x => x.ParseMessage(It.IsAny<string>())).Returns(Amazon.SimpleNotificationService.Util.Message.ParseMessage(messageText));
            _messageSignatureHandler.Setup(x => x.HandleRequest(It.IsAny<Amazon.SimpleNotificationService.Util.Message>()));
            // Act
            var result = await _service.Notify(stream);

            // Assert
            _messageSignatureHandler.Verify(mock => mock.HandleRequest(It.IsAny<Amazon.SimpleNotificationService.Util.Message>()), Times.Once);
            Assert.True(result.IsSuccess);
            Assert.Equal(messageId, result.Value);

        }

        // TODO
        //[Fact]
        //public async Task SendRawEmailNotification_MessageSignatureInvalid_ReturnFail()
        //{

        //}
    }
}
