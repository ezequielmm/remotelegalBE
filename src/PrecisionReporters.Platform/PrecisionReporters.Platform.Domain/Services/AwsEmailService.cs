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
                Source = sender == null ?_emailConfiguration.Sender : sender,
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
            var templateRequest = new GetTemplateRequest {
                TemplateName = _emailConfiguration.JoinDepositionTemplate
            };
            var template = await _emailService.GetTemplateAsync(templateRequest);
            string htmlBody = template.Template.HtmlPart;

            await SendRawEmailNotification(CreateMessageStream(deposition, participant, htmlBody));
        }

        public async Task SendRawEmailNotification(Deposition deposition)
        {
            var templateRequest = new GetTemplateRequest {
                TemplateName = _emailConfiguration.JoinDepositionTemplate
            };
            var template = await _emailService.GetTemplateAsync(templateRequest);
            string htmlBody = template.Template.HtmlPart;

            deposition.Participants.ForEach ( async x => { 
                var participantMail = x.Email ?? x.User?.EmailAddress;
                if (!string.IsNullOrEmpty(participantMail))
                    await SendRawEmailNotification(CreateMessageStream(deposition, x, htmlBody));
            });
        }

        private string AddCalendar(DateTime startDate, DateTime? endDate, string timeZone) 
        {
            var calendar = new Calendar();

            var icalEvent = new CalendarEvent
            {
                Summary = "Deposition Event",
                Description = "Description for event",
                Start = new CalDateTime(GetConvertedTime(startDate, timeZone)),
                End = endDate.HasValue ? new CalDateTime(GetConvertedTime(endDate.Value, timeZone)) : null,   
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

            mixed.Add (new TextPart(TextFormat.Html) {
                ContentTransferEncoding = ContentEncoding.Base64,
                Text = htmlBody
            });

            var ical = new TextPart ("calendar") {
                ContentTransferEncoding = ContentEncoding.Base64,
                Text = AddCalendar(deposition.StartDate, deposition.EndDate, deposition.TimeZone),
            };

            ical.ContentType.Parameters.Add ("method", "REQUEST");

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

        private string AddCalendar(DateTime startDate, DateTime? endDate = null) 
        {
            var calendar = new Calendar();

            var icalEvent = new CalendarEvent
            {
                Summary = "Deposition Event",
                Description = "Description for event",
                Start = new CalDateTime(startDate),
                End = endDate.HasValue ? new CalDateTime(endDate.Value) : null
            };

            calendar.Events.Add(icalEvent);

            var iCalSerializer = new CalendarSerializer();
            return iCalSerializer.SerializeToString(calendar);
        }

        private Multipart CreateMessageBody(Deposition deposition)
        {
            //string htmlBody = System.IO.File.ReadAllText(@"/Users/ms/MSRepos/prp-be/src/temp/JoinDepositionEmail2.html");

            var mixed = new Multipart("mixed");

            mixed.Add (new TextPart(TextFormat.Html) {
                ContentTransferEncoding = ContentEncoding.Base64,
                Text = GetHtmlBody()
            });

            var ical = new TextPart ("calendar") {
                ContentTransferEncoding = ContentEncoding.Base64,
                Text = AddCalendar(deposition.StartDate, deposition.EndDate),
            };

            ical.ContentType.Parameters.Add ("method", "REQUEST");

            mixed.Add(ical);
            return mixed;
        }

        private MimeMessage CreateMessage(Deposition deposition)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("RL Team", _emailConfiguration.Sender));
            
            deposition.Participants.ForEach ( x => {
                var participantMail = x.Email ?? x.User?.EmailAddress;
                var participantName = x.User?.GetFullName() ?? x.Name;
                if (!string.IsNullOrEmpty(participantMail))
                    message.To.Add(new MailboxAddress(participantName, participantMail));
            });
            
            message.Subject = "RL Deposition Event";

            // var builder = new BodyBuilder();

            // string htmlBody = GetHtmlBody();
            

            // builder.HtmlBody = htmlBody;


            message.Body = CreateMessageBody(deposition);
            return message;
        }

        private MemoryStream CreateMessageStream(Deposition deposition)
        {
            var stream = new MemoryStream();
            CreateMessage(deposition).WriteTo(stream);
            return stream;
        }

        private string GetHtmlBody()
        {
            return "<div style='background-color: #00304e'> <!--[if mso | IE]><table align='center' border='0' cellpadding='0' cellspacing='0' class='' " +
                "style='width:600px;' width='600' ><tr><td style='line-height:0px;font-size:0px;mso-line-height-rule:exactly;'><![endif]--> <div style=' background: white; background-color: white; margin: 0px auto; max-width: 600px; ' > <table align='center' border='0' cellpadding='0' cellspacing='0' role='presentation' style='background: white; background-color: white; width: 100%' > <tbody> <tr> <td style=' direction: ltr; font-size: 0px; padding: 42px; padding-bottom: 0; text-align: center; ' > <!--[if mso | IE]><table role='presentation' border='0' cellpadding='0' cellspacing='0'><tr><td class='' style='vertical-align:top;width:516px;' ><![endif]--> <div class='mj-column-per-100 mj-outlook-group-fix' style=' font-size: 0px; text-align: left; direction: ltr; display: inline-block; vertical-align: top; width: 100%; ' > <table border='0' cellpadding='0' cellspacing='0' role='presentation' style='vertical-align: top' width='100%' > <tr> <td align='left' style=' font-size: 0px; padding: 10px 25px; padding-right: 0; padding-bottom: 24px; padding-left: 0; word-break: break-word; ' > <table border='0' cellpadding='0' cellspacing='0' role='presentation' style='border-collapse: collapse; border-spacing: 0px' > <tbody> <tr> <td style='width: 200px'> <img height='auto' src='https://prp-dev.prdevelopment.net/static/media/logo-dark.1ab5c588.svg' " + 
                "style=' border: 0; display: block; outline: none; text-decoration: none; height: auto; width: 100%; font-size: 13px; ' width='200' /> </td> </tr> </tbody> </table> </td> </tr> <tr> <td align='left' style=' font-size: 0px; padding: 10px 25px; padding-top: 0; padding-right: 0; padding-bottom: 8px; padding-left: 0; word-break: break-word; ' > <div style=' font-family: Lato, Trebuchet, \"Trebuchet MS\", sans-serif; font-size: 16px; line-height: 1; text-align: left; color: #14232e; ' > <span>Hi <b>{{user-name}}</b></span> </div> </td> </tr> <tr> <td align='left' style=' font-size: 0px; padding: 10px 25px; padding-top: 0; padding-right: 0; padding-bottom: 0; padding-left: 0; word-break: break-word; ' > <div style=' font-family: Merriweather, Georgia, \"Times New Roman\", serif; font-size: 24px; font-weight: ligth; line-height: 30px; text-align: left; color: #14232e; ' > <span >You have been invited to the deposition of <b>{{witness-name}}</b> in <b>{{case-name}}</b>.</span > </div> </td> </tr> </table> </div> <!--[if mso | IE]></td></tr></table><![endif]--> </td> </tr> </tbody> </table> </div> <!--[if mso | IE]></td></tr></table><table align='center' border='0' cellpadding='0' cellspacing='0' class='' style='width:600px;' width='600' ><tr><td style='line-height:0px;font-size:0px;mso-line-height-rule:exactly;'><![endif]-->  " + 
                "<div style=' background: white; background-color: white; margin: 0px auto; max-width: 600px; ' > <table align='center' border='0' cellpadding='0' cellspacing='0' role='presentation' style='background: white; background-color: white; width: 100%' > <tbody> <tr> <td style=' direction: ltr; font-size: 0px; padding: 42px; padding-bottom: 0; padding-top: 24px; text-align: center; ' > <!--[if mso | IE]><table role='presentation' border='0' cellpadding='0' cellspacing='0'><tr><td class='' style='vertical-align:top;width:20px;' ><![endif]--> <div class='mj-column-px-20 mj-outlook-group-fix' style=' font-size: 0px; text-align: left; direction: ltr; display: inline-block; vertical-align: top; width: 100%; ' > <table border='0' cellpadding='0' cellspacing='0' role='presentation' style='vertical-align: top' width='100%' > <tr> <td align='left' style=' font-size: 0px; padding: 10px 25px; padding-top: 0; padding-right: 0; padding-bottom: 0; padding-left: 0; word-break: break-word; ' > <table border='0' cellpadding='0' cellspacing='0' role='presentation' style='border-collapse: collapse; border-spacing: 0px' > <tbody> <tr> <td style='width: 20px'> <img height='auto' src='data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='1em' height='1em' viewBox='0 0 24 24' fill='currentColor' stroke='%2314232E' aria-hidden='true' focusable='false' " + 
                "class=''%3E%3Cpath d='M4 3.57h16a2 2 0 012 2V20a2 2 0 01-2 2H4a2 2 0 01-2-2V5.57c0-1.1.9-2 2-2zM2.64 9.6h18.61M6.6 2v4.06M17.6 2v4.06M6.57 14.1h2.29m-2.29 3.16h2.29m2.28-3.16h2.29m-2.29 3.16h2.29m2.28-3.16H18m-2.29 3.16H18' stroke-width='1.2' fill='none' fill-rule='evenodd' stroke-linecap='round'%3E%3C/path%3E%3C/svg%3E' style=' border: 0; display: block; outline: none; text-decoration: none; height: auto; width: 100%; font-size: 13px; ' width='20' /> </td> </tr> </tbody> </table> </td> </tr> </table> </div> <!--[if mso | IE]></td><td class='' style='vertical-align:top;width:495.36px;' ><![endif]--> <div class='mj-column-per-96 mj-outlook-group-fix' style=' font-size: 0px; text-align: left; direction: ltr; display: inline-block; vertical-align: top; width: 100%; ' > <table border='0' cellpadding='0' cellspacing='0' role='presentation' width='100%' > <tbody> <tr> <td style='vertical-align: top; padding-left: 0'> <table border='0' cellpadding='0' cellspacing='0' role='presentation' width='100%' > <tr> <td align='left' style=' font-size: 0px; padding: 10px 25px; padding-top: 4px; padding-right: 0; padding-bottom: 4px; padding-left: 8px; word-break: break-word; ' > <div style=' font-family: Lato, Trebuchet, \"Trebuchet MS\", sans-serif; font-size: 12px; font-weight: bold; line-height: 12px; text-align: left; text-transform: uppercase; color: #8591a6; ' >  " + 
                " <span>DATE AND TIME OF DEPOSITION</span> </div> </td> </tr> <tr> <td align='left' style=' font-size: 0px; padding: 10px 25px; padding-top: 4px; padding-bottom: 0; padding-left: 8px; word-break: break-word; ' > <div style=' font-family: Merriweather, Georgia, \"Times New Roman\", serif; font-size: 20px; line-height: 20px; text-align: left; color: #14232e; ' > <span>{{start-date}}</span> </div> </td> </tr> </table> </td> </tr> </tbody> </table> </div> <!--[if mso | IE]></td></tr></table><![endif]--> </td> </tr> </tbody> </table> </div> <!--[if mso | IE]></td></tr></table><table align='center' border='0' cellpadding='0' cellspacing='0' class='' style='width:600px;' width='600' ><tr><td style='line-height:0px;font-size:0px;mso-line-height-rule:exactly;'><![endif]--> <div style=' background: white; background-color: white; margin: 0px auto; max-width: 600px; ' > <table align='center' border='0' cellpadding='0' cellspacing='0' role='presentation' style='background: white; background-color: white; width: 100%' > <tbody> <tr> <td style=' direction: ltr; font-size: 0px; padding: 42px; padding-top: 16px; text-align: center; ' > <!--[if mso | IE]><table role='presentation' border='0' cellpadding='0' cellspacing='0'><tr><td class='' style='vertical-align:top;width:516px;' ><![endif]--> <div class='mj-column-per-100 mj-outlook-group-fix' style=' font-size: 0px; text-align: left; direction: ltr; display: inline-block; vertical-align: top; width: 100%; ' > <table border='0' cellpadding='0' cellspacing='0' role='presentation' style='vertical-align: top' width='100%' > <tr> <td style=' font-size: 0px; padding: 10px 25px; padding-top: 8px; padding-right: 0; padding-bottom: 24px; padding-left: 0; word-break: break-word; ' > <p style=' border-top: solid 1px #d8d8d8; font-size: 1px; margin: 0px auto; width: 100%; ' ></p> <!--[if mso | IE ]><table align='center' border='0' cellpadding='0' cellspacing='0' style=' border-top: solid 1px #d8d8d8; font-size: 1px; margin: 0px auto; width: 516px; ' role='presentation' width='516px' > <tr> <td style='height: 0; line-height: 0'>&nbsp;</td> </tr> </table><! [endif]--> </td> </tr> <tr> <td align='left' style=' font-size: 0px; padding: 10px 25px; padding-top: 0; padding-right: 0; padding-bottom: 0; padding-left: 0; word-break: break-word; ' > <div style=' font-family: Lato, Trebuchet, \"Trebuchet MS\", sans-serif; font-size: 16px; line-height: 18px; text-align: left; color: #14232e; ' > <span >You can join by clicking the link below.</span > </div> </td> </tr> <tr> <td align='left' vertical-align='middle' style=' font-size: 0px; padding: 10px 25px; padding-top: 24px; padding-right: 0; padding-bottom: 32px; padding-left: 0; word-break: break-word; ' > <table border='0' cellpadding='0' cellspacing='0' role='presentation' style='border-collapse: separate; line-height: 100%' > <tr> <td align='left' style=' font-size: 0px; padding: 10px 25px; padding-top: 0; padding-right: 0; padding-bottom: 0; padding-left: 0; word-break: break-word; ' > <a href='{{join-deposition-link}}' target='_blank' style=' font-family: Lato, Trebuchet, \"Trebuchet MS\", sans-serif; color: #c09853; font-size: 14px; text-decoration: none; ' >{{join-deposition-link}}</a > </td> </tr> </table> </td> </tr> <tr> <td align='left' style=' font-size: 0px; padding: 10px 25px; padding-top: 0; padding-right: 0; padding-bottom: 0; padding-left: 0; word-break: break-word; ' > <div style=' font-family: Lato, Trebuchet, \"Trebuchet MS\", sans-serif; font-size: 14px; line-height: 18px; text-align: left; color: #14232e; ' > <span>Thank you!</span> </div> </td> </tr> <tr> <td align='left' style=' font-size: 0px; padding: 10px 25px; padding-top: 0; padding-right: 0; padding-bottom: 0; padding-left: 0; word-break: break-word; ' > <div style=' font-family: Lato, Trebuchet, \"Trebuchet MS\", sans-serif; font-size: 14px; font-weight: bold; line-height: 18px; text-align: left; color: #14232e; ' > <span>Remote Legal Team</span> </div> </td> </tr> </table> </div> <!--[if mso | IE]></td></tr></table><![endif]--> </td> </tr> </tbody> </table> </div> <!--[if mso | IE]></td></tr></table><table align='center' border='0' cellpadding='0' cellspacing='0' class='' style='width:600px;' width='600' ><tr><td style='line-height:0px;font-size:0px;mso-line-height-rule:exactly;'><![endif]--> <div style=' background: #f9f9f9; background-color: #f9f9f9; margin: 0px auto; max-width: 600px; ' > <table align='center' border='0' cellpadding='0' cellspacing='0' role='presentation' style='background: #f9f9f9; background-color: #f9f9f9; width: 100%' > " +
                " <tbody> <tr> <td style=' direction: ltr; font-size: 0px; padding: 42px; padding-bottom: 24px; padding-top: 24px; text-align: center; ' > <!--[if mso | IE]><table role='presentation' border='0' cellpadding='0' cellspacing='0'><tr><td class='' style='vertical-align:top;width:516px;' ><![endif]--> <div class='mj-column-per-100 mj-outlook-group-fix' style=' font-size: 0px; text-align: left; direction: ltr; display: inline-block; vertical-align: top; width: 100%; ' > <table border='0' cellpadding='0' cellspacing='0' role='presentation' style='vertical-align: top' width='100%' > <tr> <td align='left' style=' font-size: 0px; padding: 10px 25px; padding-top: 0; padding-right: 0; padding-bottom: 0; padding-left: 0; word-break: break-word; ' > <div style=' font-family: Lato, Trebuchet, \"Trebuchet MS\", sans-serif; font-size: 14px; line-height: 14px; text-align: left; color: #8591a6; ' > <span >Need help? Ask at <a href='mailto:help@remotelegal.com' target='_blank' style=' color: #c09853; font-size: 16px; text-decoration: none; ' >help@remotelegal.com</a ></span > </div> </td> </tr> </table> </div> <!--[if mso | IE]></td></tr></table><![endif]--> </td> </tr> </tbody> </table> </div> <!--[if mso | IE]></td></tr></table><![endif]--> </div>";

        }
    }
}
