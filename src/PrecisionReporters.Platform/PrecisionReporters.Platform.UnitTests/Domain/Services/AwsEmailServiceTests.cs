using System;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Amazon.SimpleEmail;
using Moq;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Services;
using Xunit;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.UnitTests.Utils;
using Amazon.SimpleEmail.Model;
using System.Threading;

namespace PrecisionReporters.Platform.UnitTests.Domain.Services
{
    public class AwsEmailServiceTests
    {
        private readonly Mock<ILogger<AwsEmailService>> _loggerMock;
        private readonly Mock<IAmazonSimpleEmailService> _awsSESMock;
        private readonly Mock<IOptions<EmailConfiguration>> _emailConfigurationMock;
        private readonly AwsEmailService _service;

        public AwsEmailServiceTests()
        {
            _loggerMock = new Mock<ILogger<AwsEmailService>>();
            _awsSESMock = new Mock<IAmazonSimpleEmailService>();

            var emailConfiguration = new EmailConfiguration 
            { 
                JoinDepositionTemplate = "foo",
                Sender = "sender@mail.com",
                LogoImageName = "fooImage",
                ImagesUrl = "imageUrl",
                PreDepositionLink = "someLink"
            };
            _emailConfigurationMock = new Mock<IOptions<EmailConfiguration>>();

            _awsSESMock.Setup(x => x.GetTemplateAsync(It.IsAny<GetTemplateRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetTemplateResponse { Template = new Template { HtmlPart = ""}});

            _emailConfigurationMock.Setup(x => x.Value).Returns(emailConfiguration);

            _service = new AwsEmailService(_loggerMock.Object, _awsSESMock.Object, _emailConfigurationMock.Object);
        }

        [Fact]
        public async Task SendRawEmailNotification_ShouldSendEmailForAllParticipantsWithEmail()
        {
            // Arrange
            var deposition = DepositionFactory.GetDepositionWithParticipantEmail("foo@email.com", true);
            deposition.Case = new Case 
            { 
                Name = "Case A"
            };
            deposition.StartDate = DateTime.UtcNow;
            deposition.TimeZone = "EST";

            deposition.Participants.Add(new Participant
                    {
                        Email = "witnessEmail",
                        IsAdmitted = false,
                        Role = ParticipantType.Witness
                    });
            deposition.Participants.Add(new Participant
                    {
                        IsAdmitted = false,
                        Role = ParticipantType.Observer
                    });
            
            // Act
            await _service.SendRawEmailNotification(deposition);

            // Assert
            _awsSESMock.Verify(x => x.SendRawEmailAsync(It.IsAny<SendRawEmailRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
               
        }

        [Fact]
        public async Task SendRawEmailNotification_ShouldNotSendEmailForDepositionWithoutParticipants()
        {
            // Arrange
            var deposition = DepositionFactory.GetDepositionWithoutWitness(Guid.NewGuid(), Guid.NewGuid());
            deposition.Case = new Case 
            { 
                Name = "Case A"
            };

            
            // Act
            await _service.SendRawEmailNotification(deposition);

            // Assert
            _awsSESMock.Verify(x => x.SendRawEmailAsync(It.IsAny<SendRawEmailRequest>(), It.IsAny<CancellationToken>()), Times.Never);
               
        }
    }
}
