using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.UnitTests.Utils;
using System;
using System.Threading.Tasks;
using Xunit;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class ActivityHistoryServiceTests : IDisposable
    {
        private readonly Mock<IActivityHistoryRepository> _activityRepositoryMock;
        private readonly Mock<IAwsEmailService> _awsEmailServiceMock;
        private readonly Mock<IOptions<EmailConfiguration>> _emailConfigurationMock;
        private readonly EmailConfiguration _emailConfiguration;
        private readonly Mock<IOptions<UrlPathConfiguration>> _urlPathConfigurationMock;
        private readonly UrlPathConfiguration _urlPathConfiguration;
        private readonly Mock<ILogger<ActivityHistoryService>> _loggerMock;
        private readonly ActivityHistoryService _service;
        private readonly EmailTemplateNames _emailTemplateNames;
        private readonly Mock<IOptions<EmailTemplateNames>> _emailTemplateNamesMock;
        public ActivityHistoryServiceTests()
        {
            _activityRepositoryMock = new Mock<IActivityHistoryRepository>();
            _awsEmailServiceMock = new Mock<IAwsEmailService>();

            _emailConfiguration = new EmailConfiguration { EmailNotification = "notifications@remotelegal.com" };
            _emailConfigurationMock = new Mock<IOptions<EmailConfiguration>>();
            _emailConfigurationMock.Setup(x => x.Value).Returns(_emailConfiguration);

            _urlPathConfiguration = new UrlPathConfiguration { FrontendBaseUrl = "" };
            _urlPathConfigurationMock = new Mock<IOptions<UrlPathConfiguration>>();
            _urlPathConfigurationMock.Setup(x => x.Value).Returns(_urlPathConfiguration);

            _loggerMock = new Mock<ILogger<ActivityHistoryService>>();

            _emailTemplateNames = new EmailTemplateNames { ActivityEmail = "TestEmailTemplate" };
            _emailTemplateNamesMock = new Mock<IOptions<EmailTemplateNames>>();
            _emailTemplateNamesMock.Setup(x => x.Value).Returns(_emailTemplateNames);

            _service = new ActivityHistoryService(
                _activityRepositoryMock.Object,
                _awsEmailServiceMock.Object,
                _urlPathConfigurationMock.Object,
                _emailConfigurationMock.Object,
                _loggerMock.Object,
                _emailTemplateNamesMock.Object);
        }

        public void Dispose()
        {
            // Tear down
        }

        [Fact]
        public async Task AddActivity_ShouldReturnOk_WhenActivityIsCreated()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var user = new User { EmailAddress = "test@test.com" };
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            deposition.Participants.Add(new Participant { Role = ParticipantType.Witness, Name = "Kate" });
            deposition.Case = new Case() { Name = "Case Test" };
            var activity = new ActivityHistory() { Device = "IPhone", Browser = "Safari", IPAddress = "10.10.10.10" };
            _activityRepositoryMock.Setup(x => x.Create(It.IsAny<ActivityHistory>()));
            _awsEmailServiceMock.Setup(e => e.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), It.IsAny<string>()));

            // Act
            var result = await _service.AddActivity(activity, user, deposition);

            // Assert
            _activityRepositoryMock.Verify(x => x.Create(It.IsAny<ActivityHistory>()), Times.Once);
            _awsEmailServiceMock.Verify(e => e.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), It.IsAny<string>()), Times.Once);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task AddActivity_ShouldLog_WhenExceptionOccurs()
        {
            // Arrange
            var depositionId = Guid.NewGuid();
            var caseId = Guid.NewGuid();
            var user = new User { EmailAddress = "test@test.com" };
            var deposition = DepositionFactory.GetDeposition(depositionId, caseId);
            deposition.Participants.Add(new Participant { Role = ParticipantType.Witness, Name = "Kate" });
            deposition.Case = new Case() { Name = "Case Test" };
            var activity = new ActivityHistory() { Device = "IPhone", Browser = "Safari", IPAddress = "10.10.10.10" };
            _activityRepositoryMock.Setup(x => x.Create(It.IsAny<ActivityHistory>())).ThrowsAsync(new Exception());
            _awsEmailServiceMock.Setup(e => e.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), It.IsAny<string>()));
            var logErrorMessage = "Unable to add activity";

            // Act
            var result = await _service.AddActivity(activity, user, deposition);

            // Assert
            _activityRepositoryMock.Verify(x => x.Create(It.IsAny<ActivityHistory>()), Times.Once);
            _loggerMock.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == logErrorMessage),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)), Times.Once);
            _awsEmailServiceMock.Verify(e => e.SetTemplateEmailRequest(It.IsAny<EmailTemplateInfo>(), It.IsAny<string>()), Times.Never);
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
        }
    }
}
