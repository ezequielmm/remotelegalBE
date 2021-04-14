using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using MimeKit;
using PrecisionReporters.Platform.Data.Entities;
using MimeKit.Text;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using System.Net.Http;
using Microsoft.CodeAnalysis.CSharp;
using System.Net.Mime;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class AwsEmailService : IAwsEmailService
    {
        private readonly ILogger<AwsEmailService> _logger;
        private readonly IAmazonSimpleEmailService _emailService;
        private readonly EmailConfiguration _emailConfiguration;

        public AwsEmailService(ILogger<AwsEmailService> logger, IAmazonSimpleEmailService emailService, IOptions<EmailConfiguration> emailConfiguration)
        {
            _logger = logger;
            _emailService = emailService;
            _emailConfiguration = emailConfiguration.Value;
        }

        public async Task<SendBulkTemplatedEmailResponse> SendEmailAsync(List<BulkEmailDestination> destinations, string templateName, string sender = null)
        {
            var emailRequest = new SendBulkTemplatedEmailRequest
            {
                Source = sender == null ? _emailConfiguration.Sender : sender,
                Template = templateName,
                DefaultTemplateData = destinations[0].ReplacementTemplateData,
                Destinations = destinations
            };

            var response = await _emailService.SendBulkTemplatedEmailAsync(emailRequest);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation($"The email with message Id {response.Status[0].MessageId} sent successfully on {DateTime.UtcNow:O}");
            }
            else
            {
                _logger.LogError($"Failed to send email with message Id {response.Status[0].MessageId} on {DateTime.UtcNow:O} due to {response.HttpStatusCode}.");
            }
            return response;
        }

        public async Task SetTemplateEmailRequest(EmailTemplateInfo emailData, string sender = null)
        {
            var destinations = new List<BulkEmailDestination>
            {
                new BulkEmailDestination
                {
                    Destination = new Destination(emailData.EmailTo),
                    ReplacementTemplateData = JsonSerializer.Serialize(emailData.TemplateData)
                }
            };

            await SendEmailAsync(destinations, emailData.TemplateName, sender);
        }

        public async Task SendRawEmailNotification(MemoryStream streamMessage)
        {
            var sendRequest = new SendRawEmailRequest { RawMessage = new RawMessage(streamMessage) };
            try
            {
                _logger.LogDebug("Sending email using Amazon SES.");
                var r = await _emailService.SendRawEmailAsync(sendRequest);
                _logger.LogDebug("The email was sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError("The email was not sent.");
                _logger.LogError($"Error message: {ex.Message}");
            }
        }

        public async Task SendRawEmailNotification(Deposition deposition, Participant participant)
        {
            var templateRequest = new GetTemplateRequest
            {
                TemplateName = _emailConfiguration.JoinDepositionTemplate
            };
            var template = await _emailService.GetTemplateAsync(templateRequest);
            string htmlBody = template.Template.HtmlPart;

            await SendRawEmailNotification(CreateMessageStream(deposition, participant, htmlBody));
        }

        public async Task SendRawEmailNotification(Deposition deposition)
        {
            var templateRequest = new GetTemplateRequest
            {
                TemplateName = _emailConfiguration.JoinDepositionTemplate
            };
            var template = await _emailService.GetTemplateAsync(templateRequest);
            string htmlBody = template.Template.HtmlPart;

            deposition.Participants.ForEach(async x =>
            {
                var participantMail = x.Email ?? x.User?.EmailAddress;
                if (!string.IsNullOrEmpty(participantMail))
                    await SendRawEmailNotification(CreateMessageStream(deposition, x, htmlBody));
            });
        }

        private string AddCalendar(DateTime startDate, DateTime? endDate, Deposition deposition)
        {
            var calendar = new Calendar();
            calendar.Method = "REQUEST";
            var icalEvent = new CalendarEvent
            {
                Uid = deposition.Id.ToString(),
                Summary = deposition.Case.Name,
                Description = $"You can join by clicking the link {_emailConfiguration.PreDepositionLink}{deposition.Id}",
                Start = new CalDateTime(GetConvertedTime(startDate, deposition.TimeZone)),
                End = endDate.HasValue ? new CalDateTime(GetConvertedTime(endDate.Value, deposition.TimeZone)) : null
            };
            calendar.Events.Add(icalEvent);
            var iCalSerializer = new CalendarSerializer();
            return iCalSerializer.SerializeToString(calendar);
        }

        private Multipart CreateMessageBody(Deposition deposition, string participantName, string witnessName, string htmlBodyTemplate)
        {
            var mixed = new Multipart("mixed");
            var caseName = deposition.Case.Name;
            var imageUrl = GetImageUrl(_emailConfiguration.LogoImageName);
            var htmlBody = htmlBodyTemplate
                .Replace("{{dateAndTime}}", $"{GetConvertedTime(deposition.StartDate, deposition.TimeZone):MMMM d, yyyy hh:mm tt}")
                .Replace("{{name}}", participantName)
                .Replace("{{imageUrl}}", imageUrl)
                .Replace("{{depositionJoinLink}}", $"{_emailConfiguration.PreDepositionLink}{deposition.Id}");

            if (string.IsNullOrEmpty(witnessName))
                htmlBody = htmlBody.Replace("{{case}}", caseName);
            else
                htmlBody = htmlBody.Replace("{{case}}", $"{witnessName} in {caseName}");

            mixed.Add(new TextPart(TextFormat.Html)
            {
                ContentTransferEncoding = ContentEncoding.Base64,
                Text = htmlBody
            });

            var ical = new TextPart("calendar")
            {
                ContentTransferEncoding = ContentEncoding.SevenBit,
                Text = AddCalendar(deposition.StartDate, deposition.EndDate, deposition),
            };

            ical.ContentType.Parameters.Add("method", "REQUEST");

            mixed.Add(ical);
            return mixed;
        }

        private MimeMessage CreateMessage(Deposition deposition, Participant participant, string htmlBodyTemplate)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Remote Legal Team", _emailConfiguration.Sender));

            var participantMail = participant.Email ?? participant.User?.EmailAddress;
            var participantName = participant.User?.GetFullName() ?? participant.Name;

            message.To.Add(new MailboxAddress(participantName, participantMail));

            var witnessName = deposition.Participants.Single(x => x.Role == Data.Enums.ParticipantType.Witness)?.Name;
            message.Subject = $"Invitation: Remote Legal - {witnessName} - {deposition.Case.Name} - {GetConvertedTime(deposition.StartDate, deposition.TimeZone):MMMM d, yyyy hh:mm tt}";

            message.Body = CreateMessageBody(deposition, participantName ?? "", witnessName, htmlBodyTemplate);
            return message;
        }

        private MemoryStream CreateMessageStream(Deposition deposition, Participant participant, string htmlBodyTemplate)
        {
            var stream = new MemoryStream();
            CreateMessage(deposition, participant, htmlBodyTemplate).WriteTo(stream);
            return stream;
        }

        private string GetImageUrl(string name)
        {
            return $"{_emailConfiguration.ImagesUrl}{name}";
        }

        private DateTime GetConvertedTime(DateTime dateTime, string timeZone)
        {
            return TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
        }
    }
}
