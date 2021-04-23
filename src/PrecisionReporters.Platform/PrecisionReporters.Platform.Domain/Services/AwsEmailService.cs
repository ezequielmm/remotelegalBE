using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

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

            _logger.LogInformation($"Sended template data: {string.Join("\n", emailData.TemplateData.Select(x => x.Key + ": " + x.Value))}");
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

        // TODO: This method is not agnostic from the business so it shouldn't be on this class
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

        // TODO: This method is not agnostic from the business so it shouldn't be on this class
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

        // TODO: This method is not agnostic from the business so it shouldn't be on this class
        private string AddCalendar(Deposition deposition)
        {
            var witness = deposition.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
            var strWitness = !string.IsNullOrWhiteSpace(witness.Name) ? $"{witness.Name} - {deposition.Case.Name} " : deposition.Case.Name;
            var calendar = new Calendar();
            calendar.Method = "REQUEST";
            var icalEvent = new CalendarEvent
            {
                Uid = deposition.Id.ToString(),
                Summary = $"Invitation: Remote Legal - {strWitness}",
                Description = $"{strWitness}{Environment.NewLine}{_emailConfiguration.PreDepositionLink}{deposition.Id}",
                Start = new CalDateTime(deposition.StartDate.GetConvertedTime(deposition.TimeZone), deposition.TimeZone),
                End = deposition.EndDate.HasValue ? new CalDateTime(deposition.EndDate.Value.GetConvertedTime(deposition.TimeZone), deposition.TimeZone) : null,
                Location = $"{_emailConfiguration.PreDepositionLink}{deposition.Id}",
                Organizer = new Organizer(_emailConfiguration.Sender)
            };
            calendar.Events.Add(icalEvent);
            var iCalSerializer = new CalendarSerializer();
            return iCalSerializer.SerializeToString(calendar);
        }

        // TODO: This method is not agnostic from the business so it shouldn't be on this class
        private Multipart CreateMessageBody(Deposition deposition, string participantName, string witnessName, string htmlBodyTemplate)
        {
            var mixed = new Multipart("mixed");
            var caseName = deposition.Case.Name;
            var htmlBody = htmlBodyTemplate
                .Replace("{{dateAndTime}}", $"{deposition.StartDate.GetFormattedDateTime(deposition.TimeZone)}")
                .Replace("{{name}}", participantName)
                .Replace("{{imageUrl}}", GetImageUrl(_emailConfiguration.LogoImageName))
                .Replace("{{calendar}}", GetImageUrl(_emailConfiguration.CalendarImageName))
                .Replace("{{depositionJoinLink}}", $"{_emailConfiguration.PreDepositionLink}{deposition.Id}");

            if (string.IsNullOrEmpty(witnessName))
                htmlBody = htmlBody.Replace("{{case}}", caseName);
            else
                htmlBody = htmlBody.Replace("{{case}}", $"{witnessName} in {caseName}");

            var alternative = new MultipartAlternative();
            alternative.Add(new TextPart(TextFormat.Plain)
            {
                ContentTransferEncoding = ContentEncoding.QuotedPrintable,
                ContentDisposition = new ContentDisposition()
                {
                    Disposition = System.Net.Mime.DispositionTypeNames.Inline,
                    IsAttachment = false,
                },
                Text = $"You can join by clicking the link: {_emailConfiguration.PreDepositionLink}{deposition.Id}"
            });
            alternative.Add(new TextPart(TextFormat.Html)
            {
                ContentTransferEncoding = ContentEncoding.QuotedPrintable,
                Text = htmlBody
            });
            
            mixed.Add(alternative);

            var ical = new TextPart("calendar")
            {
                ContentTransferEncoding = ContentEncoding.SevenBit,
                Text = AddCalendar(deposition)
            };
            ical.ContentType.Parameters.Add("method", "REQUEST");

            mixed.Add(ical);
            return mixed;
        }

        // TODO: This method is not agnostic from the business so it shouldn't be on this class
        private MimeMessage CreateMessage(Deposition deposition, Participant participant, string htmlBodyTemplate)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Remote Legal Team", _emailConfiguration.EmailNotification));

            var participantMail = participant.Email ?? participant.User?.EmailAddress;
            var participantName = participant.User?.GetFullName() ?? participant.Name;

            message.To.Add(new MailboxAddress(participantName, participantMail));

            var witnessName = deposition.Participants.Single(x => x.Role == Data.Enums.ParticipantType.Witness)?.Name;
            var strWitnesTitle = !string.IsNullOrWhiteSpace(witnessName) ? $"- {witnessName} " : string.Empty;
            message.Subject = message.Subject = $"Invitation: Remote Legal {witnessName}- {deposition.Case.Name} - {deposition.StartDate.GetFormattedDateTime(deposition.TimeZone)}";
            message.Body = CreateMessageBody(deposition, participantName ?? "", witnessName, htmlBodyTemplate);
            
            return message;
        }

        // TODO: This method is not agnostic from the business so it shouldn't be on this class
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
    }
}
