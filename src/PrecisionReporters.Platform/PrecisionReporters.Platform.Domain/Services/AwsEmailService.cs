using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class AwsEmailService : IAwsEmailService
    {
        private readonly ILogger<AwsEmailService> _logger;
        private readonly CognitoConfiguration _cognitoConfiguration;
        private readonly EmailConfiguration _emailConfiguration;
        private IAmazonSimpleEmailService _emailService;
        private readonly string _filePath;

        public AwsEmailService(ILogger<AwsEmailService> logger, IOptions<CognitoConfiguration> cognitoConfiguration, IOptions<EmailConfiguration> emailConfiguration, IHostingEnvironment env, IAmazonSimpleEmailService emailService)
        {
            _logger = logger;
            _cognitoConfiguration = cognitoConfiguration.Value;
            _emailConfiguration = emailConfiguration.Value;
            _filePath = env.ContentRootPath;
            _emailService = emailService;
        }

        public async Task<SendRawEmailResponse> SendEmailAsync(EmailTemplateInfo emailTemplateInfo, string emailTo, List<string> cc = null, List<string> bcc = null)
        {
            var emailMessage = BuildEmailHeaders(_emailConfiguration.Sender, emailTo, cc, bcc, _emailConfiguration.VerifyEmailSubject);
            var emailBody = BuildEmailBody(emailTemplateInfo);
            emailMessage.Body = emailBody.ToMessageBody();
            return await SendEmailAsync(emailMessage);
        }

        private BodyBuilder BuildEmailBody(EmailTemplateInfo emailTemplateInfo)
        {
            var bodyBuilder = new BodyBuilder();
            var emailTemplate = Path.Combine(_filePath, $"{_emailConfiguration.BaseTemplatePath}{emailTemplateInfo.TemplateName}");

            var verifyEmailTemplate = File.ReadAllText(emailTemplate);
            var helpEmail = _emailConfiguration.EmailHelp;
            //bodyBuilder.HtmlBody = string.Format(verifyEmailTemplate, emailTemplateInfo.TemplateData.ToArray());
            var dataArray = emailTemplateInfo.TemplateData.ToArray();
            var baseUrl = "https://prdevelopment.net/";
            var link = $"{baseUrl}{dataArray[2]}";
            bodyBuilder.HtmlBody = @$"<table>
                                        <tr>
                                            <td>
                                                Verification Link
                                            </td>
                                            <td>
                                                {link}
                                            </td>
                                        </tr>
                                    </table>";

            return bodyBuilder;
        }

        private MimeMessage BuildEmailHeaders(string from, string emailTo, List<string> cc, List<string> bcc, string subject)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(string.Empty, from));
            message.To.Add(new MailboxAddress(string.Empty, emailTo));

            cc?.ForEach(i => message.Cc.Add(new MailboxAddress(string.Empty, i)));   
            
            bcc?.ForEach(i => message.Bcc.Add(new MailboxAddress(string.Empty, i)));
            
            message.Subject = subject;
            return message;
        }

        private async Task<SendRawEmailResponse> SendEmailAsync(MimeMessage message)
        {
            using (var memoryStream = new MemoryStream())
            {
                await message.WriteToAsync(memoryStream);
                var sendRequest = new SendRawEmailRequest { RawMessage = new RawMessage(memoryStream) };

                var response = await _emailService.SendRawEmailAsync(sendRequest);
                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    _logger.LogInformation($"The email with message Id {response.MessageId} sent successfully to {message.To} on {DateTime.UtcNow:O}");
                }
                else
                {
                    _logger.LogError($"Failed to send email with message Id {response.MessageId} to {message.To} on {DateTime.UtcNow:O} due to {response.HttpStatusCode}.");
                }
                return response;
            }
        }
    }
}
