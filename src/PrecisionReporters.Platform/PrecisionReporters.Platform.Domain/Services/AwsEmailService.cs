using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Ical.Net.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
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
                await _emailService.SendRawEmailAsync(sendRequest);
                _logger.LogDebug("The email was sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError("The email was not sent.");
                _logger.LogError($"Error message: {ex.Message}");
            }
        }

        public async Task SendRawEmailNotification(EmailTemplateInfo emailTemplateInfo)
        {
            var templateRequest = new GetTemplateRequest
            {
                TemplateName = emailTemplateInfo.TemplateName
            };
            var template = await _emailService.GetTemplateAsync(templateRequest);
            string htmlBody = template.Template.HtmlPart;
            await SendRawEmailNotification(CreateMessageStream(emailTemplateInfo, htmlBody));
        }
        
        private Multipart CreateMessageBody(EmailTemplateInfo emailTemplateInfo, string htmlBodyTemplate)
        {
            var mixed = new Multipart("mixed");
            foreach (var key in emailTemplateInfo.TemplateData.Keys)
            {
                var text = htmlBodyTemplate.Replace($"{{{{{key}}}}}", emailTemplateInfo.TemplateData.GetValueOrDefault(key));
                htmlBodyTemplate = text;
            }

            var alternative = new MultipartAlternative
            {
                new TextPart(TextFormat.Plain)
                {
                    ContentTransferEncoding = ContentEncoding.QuotedPrintable,
                    ContentDisposition = new ContentDisposition()
                    {
                        Disposition = System.Net.Mime.DispositionTypeNames.Inline,
                        IsAttachment = false,
                    },
                    Text = emailTemplateInfo.AddiotionalText
                },
                new TextPart(TextFormat.Html)
                {
                    ContentTransferEncoding = ContentEncoding.QuotedPrintable,
                    Text = htmlBodyTemplate
                }
            };

            mixed.Add(alternative);

            var calendar = emailTemplateInfo.Calendar;
            var iCalSerializer = new CalendarSerializer();
            var ical = new TextPart("calendar")
            {
                ContentTransferEncoding = ContentEncoding.SevenBit,
                Text = iCalSerializer.SerializeToString(calendar)
            };
            ical.ContentType.Parameters.Add("method", calendar.Method);

            mixed.Add(ical);
            return mixed;
        }

        private MimeMessage CreateMessage(EmailTemplateInfo emailTemplateInfo, string htmlBodyTemplate)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Remote Legal Team", _emailConfiguration.EmailNotification));

            message.To.Add(MailboxAddress.Parse(emailTemplateInfo.EmailTo.FirstOrDefault()));

            var subject = emailTemplateInfo.Subject;
            message.Subject = subject;

            message.Body = CreateMessageBody(emailTemplateInfo, htmlBodyTemplate);
            
            return message;
        }

        private MemoryStream CreateMessageStream(EmailTemplateInfo emailTemplateInfo, string htmlBodyTemplate)
        {
            var stream = new MemoryStream();
            CreateMessage(emailTemplateInfo, htmlBodyTemplate).WriteTo(stream);
            return stream;
        }
    }
}
