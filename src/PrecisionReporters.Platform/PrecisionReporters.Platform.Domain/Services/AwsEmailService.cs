using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
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

        public async Task<SendBulkTemplatedEmailResponse> SendEmailAsync(List<BulkEmailDestination> destinations, string templateName)
        {
            var emailRequest = new SendBulkTemplatedEmailRequest
            {
                Source = _emailConfiguration.Sender,
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

        public async Task SetTemplateEmailRequest(EmailTemplateInfo emailData)
        {
            var destinations = new List<BulkEmailDestination>
            {
                new BulkEmailDestination
                {
                    Destination = new Destination(emailData.EmailTo),
                    ReplacementTemplateData = JsonSerializer.Serialize(emailData.TemplateData)
                }
            };

            await SendEmailAsync(destinations, emailData.TemplateName);
        }
    }
}
