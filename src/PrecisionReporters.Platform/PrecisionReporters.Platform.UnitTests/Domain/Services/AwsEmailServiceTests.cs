using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Services;
using PrecisionReporters.Platform.Shared.Commons;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

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
                EmailNotification = "notifications@mail.com",
                LogoImageName = "fooImage",
                ImagesUrl = "imageUrl",
                CalendarImageName = "calendarImageUrl",
                PreDepositionLink = "someLink"
            };
            _emailConfigurationMock = new Mock<IOptions<EmailConfiguration>>();

            _awsSESMock.Setup(x => x.GetTemplateAsync(It.IsAny<GetTemplateRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetTemplateResponse { Template = new Template { HtmlPart = ""}});

            _emailConfigurationMock.Setup(x => x.Value).Returns(emailConfiguration);

            _service = new AwsEmailService(_loggerMock.Object, _awsSESMock.Object, _emailConfigurationMock.Object);
        }

        [Fact]
        public async Task SendRawEmailNotification_ShouldSendEmail()
        {
            // Arrange
            var emailTemplateInfo = new EmailTemplateInfo();
            emailTemplateInfo.AdditionalText = "";
            emailTemplateInfo.TemplateData = new Dictionary<string, string>() { { "test", "test" } };
            emailTemplateInfo.TemplateName = "";
            emailTemplateInfo.Subject = "";
            emailTemplateInfo.EmailTo = new List<string> { "test@test.com" };
            var calendar = new Calendar();
            calendar.Method = "REQUEST";
            var timeZone = "America/New_York";
            var icalEvent = new CalendarEvent
            {
                Uid = Guid.NewGuid().ToString(),
                Summary = "",
                Description = "",
                Start = new CalDateTime(DateTime.UtcNow.GetConvertedTime(timeZone), timeZone),
                End = null,
                Location = "",
                Organizer = new Organizer
                {
                    CommonName = "Organizer Name",
                    Value = new Uri("mailto: organizer@test.com")
                }
            };

            calendar.Events.Add(icalEvent);
            emailTemplateInfo.Calendar = calendar;

            // Act
            await _service.SendRawEmailNotification(emailTemplateInfo);

            // Assert
            _awsSESMock.Verify(x => x.SendRawEmailAsync(It.IsAny<SendRawEmailRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
               
        }

    }
}
