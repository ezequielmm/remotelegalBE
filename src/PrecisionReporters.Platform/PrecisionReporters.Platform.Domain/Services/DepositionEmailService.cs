﻿using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Enums;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Shared.Helpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class DepositionEmailService : IDepositionEmailService
    {
        private readonly IAwsEmailService _awsEmailService;
        private readonly EmailConfiguration _emailConfiguration;
        private readonly ILogger<DepositionEmailService> _logger;
        private readonly ILoggingHelper _loggingHelper;

        public DepositionEmailService(IAwsEmailService awsEmailService,
            IOptions<UrlPathConfiguration> urlPathConfiguration,
            ILogger<DepositionEmailService> logger,
            IOptions<EmailConfiguration> emailConfiguration,
            ILoggingHelper loggingHelper)
        {
            _awsEmailService = awsEmailService;
            _logger = logger;
            _emailConfiguration = emailConfiguration.Value;
            _loggingHelper = loggingHelper;
        }
        public async Task SendJoinDepositionEmailNotification(Deposition deposition)
        {
            var tasks = deposition.Participants.Where(p => !string.IsNullOrWhiteSpace(p.Email)).Select(participant =>
            {
                var template = GetJoinDepositionEmailTemplate(deposition, participant);
                _loggingHelper.LogInformationWithScope(LogCategory.JoinNotification, $"Sending Join Deposition Email Notification to {participant.Email}");
                return _awsEmailService.SendRawEmailNotification(template);
            });

            await Task.WhenAll(tasks);
        }

        public async Task SendJoinDepositionEmailNotification(Deposition deposition, Participant participant)
        {
            await _loggingHelper.LogInformationWithScope(LogCategory.JoinNotification, $"Sending Join Deposition Email Notification to {participant.Email}");
            var template = GetJoinDepositionEmailTemplate(deposition, participant);
            await _awsEmailService.SendRawEmailNotification(template);
        }

        public async Task SendCancelDepositionEmailNotification(Deposition deposition, Participant participant)
        {
            await _loggingHelper.LogInformationWithScope(LogCategory.CancelNotification, $"Sending Cancel Deposition Email Notification to {participant.Email}");
            var template = GetCancelDepositionEmailTemplate(deposition, participant);
            await _awsEmailService.SendRawEmailNotification(template);
        }

        public async Task SendRescheduleDepositionEmailNotification(Deposition deposition, Participant participant, DateTime oldStartDate, string oldTimeZone)
        {
            await _loggingHelper.LogInformationWithScope(LogCategory.RescheduleNotification, $"Sending Reschedule Deposition Email Notification to {participant.Email}");
            var template = GetReScheduleDepositionEmailTemplate(deposition, participant, oldStartDate, oldTimeZone);
            await _awsEmailService.SendRawEmailNotification(template);
        }

        public async Task SendDepositionReminder(Deposition deposition, Participant participant)
        {
            await _loggingHelper.LogInformationWithScope(LogCategory.ReminderNotification, $"Sending Reminder Deposition Email Notification to {participant.Email}");
            var template = GetDepositionReminderEmailTemplate(deposition, participant);
            await _awsEmailService.SendRawEmailNotification(template);
        }

        private EmailTemplateInfo GetJoinDepositionEmailTemplate(Deposition deposition, Participant participant)
        {
            var template = new EmailTemplateInfo
            {
                EmailTo = new List<string> { participant.Email },
                TemplateData = new Dictionary<string, string>
                            {
                                { "dateAndTime", deposition.StartDate.GetFormattedDateTime(deposition.TimeZone) },
                                { "name", participant.Name ?? string.Empty },
                                { "case", GetDescriptionCase(deposition) },
                                { "imageUrl",  GetImageUrl(_emailConfiguration.LogoImageName) },
                                { "calendar", GetImageUrl(_emailConfiguration.CalendarImageName) },
                                { "depositionJoinLink", $"{_emailConfiguration.PreDepositionLink}{deposition.Id}"}
                            },
                TemplateName = _emailConfiguration.JoinDepositionTemplate,
                Calendar = CreateCalendar(deposition, CalendarAction.Add.GetDescription()),
                AdditionalText = $"You can join by clicking the link: {_emailConfiguration.PreDepositionLink}{deposition.Id}",
                Subject = $"Invitation: Remote Legal - {GetSubject(deposition)}"
            };

            return template;
        }

        private EmailTemplateInfo GetCancelDepositionEmailTemplate(Deposition deposition, Participant participant)
        {
            var template = new EmailTemplateInfo
            {
                EmailTo = new List<string> { participant.Email },
                TemplateData = new Dictionary<string, string>
                            {
                                { "start-date", deposition.StartDate.GetFormattedDateTime(deposition.TimeZone) },
                                { "user-name", participant.Name },
                                { "case-name", GetDescriptionCase(deposition) },
                                { "images-url",  _emailConfiguration.ImagesUrl },
                                { "logo", GetImageUrl(_emailConfiguration.LogoImageName) }
                            },
                TemplateName = ApplicationConstants.CancelDepositionEmailTemplate,
                AdditionalText = string.Empty,
                Calendar = CreateCalendar(deposition, CalendarAction.Cancel.GetDescription()),
                Subject = $"Cancellation: Remote Legal - {GetSubject(deposition)}"
            };

            return template;
        }

        private EmailTemplateInfo GetReScheduleDepositionEmailTemplate(Deposition deposition, Participant participant, DateTime oldStartDate, string oldTimeZone)
        {
            var template = new EmailTemplateInfo
            {
                EmailTo = new List<string> { participant.Email },
                TemplateData = new Dictionary<string, string>
                            {
                                { "old-start-date", oldStartDate.GetFormattedDateTime(oldTimeZone) },
                                { "start-date", deposition.StartDate.GetFormattedDateTime(deposition.TimeZone) },
                                { "user-name", participant.Name ?? string.Empty },
                                { "case-name", GetDescriptionCase(deposition) },
                                { "images-url",  _emailConfiguration.ImagesUrl },
                                { "logo", GetImageUrl(_emailConfiguration.LogoImageName) },
                                { "deposition-join-link", $"{_emailConfiguration.PreDepositionLink}{deposition.Id}"}
                            },
                TemplateName = ApplicationConstants.ReScheduleDepositionEmailTemplate,
                AdditionalText = $"You can join by clicking the link: {_emailConfiguration.PreDepositionLink}{deposition.Id}",
                Calendar = CreateCalendar(deposition, CalendarAction.Update.GetDescription()),
                Subject = $"Invitation update: Remote Legal - {GetSubject(deposition)}"
            };

            return template;
        }

        private EmailTemplateInfo GetDepositionReminderEmailTemplate(Deposition deposition, Participant participant)
        {
            var template = new EmailTemplateInfo
            {
                EmailTo = new List<string> { participant.Email },
                TemplateData = new Dictionary<string, string>
                            {
                                { "dateAndTime", deposition.StartDate.GetFormattedDateTime(deposition.TimeZone) },
                                { "name", participant.Name ?? string.Empty },
                                { "case", GetDescriptionCase(deposition) },
                                { "imageUrl",  GetImageUrl(_emailConfiguration.LogoImageName) },
                                { "calendar", GetImageUrl(_emailConfiguration.CalendarImageName) },
                                { "depositionJoinLink", $"{_emailConfiguration.PreDepositionLink}{deposition.Id}"}
                            },
                TemplateName = ApplicationConstants.DepositionReminderEmailTemplate,
                AdditionalText = $"You can join by clicking the link: {_emailConfiguration.PreDepositionLink}{deposition.Id}",
                Subject = $"Invitation reminder: Remote Legal - {GetSubject(deposition)}"
            };

            return template;
        }

        private string GetSubject(Deposition deposition)
        {
            var witness = deposition.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
            var subject = $"{deposition.Case.Name} - {deposition.StartDate.GetFormattedDateTime(deposition.TimeZone)}";

            if (!string.IsNullOrEmpty(witness?.Name))
                subject = $"{witness.Name} - {deposition.Case.Name} - {deposition.StartDate.GetFormattedDateTime(deposition.TimeZone)}";

            return subject;
        }

        private string GetDescriptionCase(Deposition deposition)
        {
            var witness = deposition.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
            var caseName = $"<b>{deposition.Case.Name}</b>";

            if (!string.IsNullOrEmpty(witness?.Name))
                caseName = $"<b>{witness.Name}</b> in the case of <b>{caseName}</b>";

            return caseName;
        }

        private string GetImageUrl(string name)
        {
            return $"{_emailConfiguration.ImagesUrl}{name}";
        }

        private Calendar CreateCalendar(Deposition deposition, string method = "REQUEST")
        {
            var witness = deposition.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
            var strWitness = !string.IsNullOrWhiteSpace(witness?.Name) ? $"{witness.Name} - {deposition.Case.Name} " : deposition.Case.Name;
            var calendar = new Calendar
            {
                Method = method
            };

            var icalEvent = new CalendarEvent
            {
                Uid = deposition.Id.ToString(),
                Summary = $"Invitation: Remote Legal - {strWitness}",
                Description = $"{strWitness}{Environment.NewLine}{_emailConfiguration.PreDepositionLink}{deposition.Id}",
                Start = new CalDateTime(deposition.StartDate.GetConvertedTime(deposition.TimeZone), deposition.TimeZone),
                End = deposition.EndDate.HasValue ? new CalDateTime(deposition.EndDate.Value.GetConvertedTime(deposition.TimeZone), deposition.TimeZone) : null,
                Location = $"{_emailConfiguration.PreDepositionLink}{deposition.Id}",
                Organizer = new Organizer()
                {
                    CommonName = _emailConfiguration.SenderLabel,
                    Value = new Uri($"mailto:{_emailConfiguration.EmailNotification}")
                }
            };

            calendar.Events.Add(icalEvent);

            return calendar;
        }
    }
}
