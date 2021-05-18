using Microsoft.Extensions.Options;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
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
    public class DepositionEmailServiceTests : IDisposable
    {

        private readonly Mock<IAwsEmailService> _awsEmailServiceMock;
        private readonly DepositionEmailService _service;
        private readonly EmailConfiguration _emailConfiguration;
        private readonly UrlPathConfiguration _urlPathConfiguration;
        private readonly Mock<IOptions<UrlPathConfiguration>> _urlPathConfigurationMock;
        private readonly Mock<IOptions<EmailConfiguration>> _emailConfigurationMock;
        public DepositionEmailServiceTests()
        {
            _emailConfiguration = new EmailConfiguration { EmailNotification = "notifications@remotelegal.com", PreDepositionLink = "", LogoImageName = "", ImagesUrl = "" };
            _emailConfigurationMock = new Mock<IOptions<EmailConfiguration>>();
            _emailConfigurationMock.Setup(x => x.Value).Returns(_emailConfiguration);

            _urlPathConfiguration = new UrlPathConfiguration { FrontendBaseUrl = "" };
            _urlPathConfigurationMock = new Mock<IOptions<UrlPathConfiguration>>();
            _urlPathConfigurationMock.Setup(x => x.Value).Returns(_urlPathConfiguration);

            _awsEmailServiceMock = new Mock<IAwsEmailService>();

            _service = new DepositionEmailService(_awsEmailServiceMock.Object,_urlPathConfigurationMock.Object,_emailConfigurationMock.Object);

        }

        [Fact]
        public async Task SendJoinDepositionEmailNotification_ShouldSendEmail_WithoutWitness()
        {
            //Arrange
            var deposition = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            deposition.Case = new Case() { Name = "Test" };
            _awsEmailServiceMock.Setup(x => x.SendRawEmailNotification(It.IsAny<EmailTemplateInfo>()));
            
            //Act
            await _service.SendJoinDepositionEmailNotification(deposition);

            //Assert
            _awsEmailServiceMock.Verify(x => x.SendRawEmailNotification(It.IsAny<EmailTemplateInfo>()), Times.Exactly(deposition.Participants.Count));
        }

        [Fact]
        public async Task SendJoinDepositionEmailNotification_ShouldSendEmail_WithWitness()
        {
            //Arrange
            var deposition = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            var witness = new Participant() { Email = "witnes@test.com", Name = "Jhon", Role = ParticipantType.Witness };
            deposition.Participants.Add(witness);
            deposition.Case = new Case() { Name = "Test" };
            _awsEmailServiceMock.Setup(x => x.SendRawEmailNotification(It.IsAny<EmailTemplateInfo>()));

            //Act
            await _service.SendJoinDepositionEmailNotification(deposition);

            //Assert
            _awsEmailServiceMock.Verify(x => x.SendRawEmailNotification(It.IsAny<EmailTemplateInfo>()), Times.Exactly(deposition.Participants.Count));
        }

        [Fact]
        public async Task SendCancelDepositionEmailNotification_ShouldSendEmail_WithoutWitness()
        {
            //Arrange
            var deposition = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            deposition.Case = new Case() { Name = "Test" };
            var participant = deposition.Participants[0];
            _awsEmailServiceMock.Setup(x => x.SendRawEmailNotification(It.IsAny<EmailTemplateInfo>()));

            //Act
            await _service.SendCancelDepositionEmailNotification(deposition, participant);

            //Assert
            _awsEmailServiceMock.Verify(x => x.SendRawEmailNotification(It.IsAny<EmailTemplateInfo>()), Times.Once);
        }

        [Fact]
        public async Task SendCancelDepositionEmailNotification_ShouldSendEmail_WithWitness()
        {
            //Arrange
            var deposition = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            var witness = new Participant() { Email = "witnes@test.com", Name = "Jhon", Role = ParticipantType.Witness };
            deposition.Participants.Add(witness);
            deposition.Case = new Case() { Name = "Test" };
            var participant = deposition.Participants[0];
            _awsEmailServiceMock.Setup(x => x.SendRawEmailNotification(It.IsAny<EmailTemplateInfo>()));

            //Act
            await _service.SendCancelDepositionEmailNotification(deposition, participant);

            //Assert
            _awsEmailServiceMock.Verify(x => x.SendRawEmailNotification(It.IsAny<EmailTemplateInfo>()), Times.Once);
        }

        [Fact]
        public async Task SendReSheduleDepositionEmailNotification_ShouldSendEmail_WithoutWitness()
        {
            //Arrange
            var deposition = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            deposition.Case = new Case() { Name = "Test" };
            var participant = deposition.Participants[0];
            var oldDate = DateTime.UtcNow.AddDays(-1);
            _awsEmailServiceMock.Setup(x => x.SendRawEmailNotification(It.IsAny<EmailTemplateInfo>()));

            //Act
            await _service.SendReSheduleDepositionEmailNotification(deposition, participant, oldDate, "America/New_York");

            //Assert
            _awsEmailServiceMock.Verify(x => x.SendRawEmailNotification(It.IsAny<EmailTemplateInfo>()), Times.Once);
        }

        [Fact]
        public async Task SendReSheduleDepositionEmailNotification_ShouldSendEmail_WithWitness()
        {
            //Arrange
            var deposition = DepositionFactory.GetDeposition(Guid.NewGuid(), Guid.NewGuid());
            var witness = new Participant() { Email = "witnes@test.com", Name = "Jhon", Role = ParticipantType.Witness };
            deposition.Participants.Add(witness);
            deposition.Case = new Case() { Name = "Test" };
            var participant = deposition.Participants[0];
            var oldDate = DateTime.UtcNow.AddDays(-1);
            _awsEmailServiceMock.Setup(x => x.SendRawEmailNotification(It.IsAny<EmailTemplateInfo>()));

            //Act
            await _service.SendReSheduleDepositionEmailNotification(deposition, participant, oldDate, "America/New_York");

            //Assert
            _awsEmailServiceMock.Verify(x => x.SendRawEmailNotification(It.IsAny<EmailTemplateInfo>()), Times.Once);
        }

        public void Dispose()
        {

        }
    }
}
