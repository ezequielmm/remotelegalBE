using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class ActivityHistoryService : IActivityHistoryService
    {
        private readonly IActivityHistoryRepository _activityHistoryRepository;
        private readonly IAwsEmailService _awsEmailService;
        private readonly UrlPathConfiguration _urlPathConfiguration;
        private readonly EmailConfiguration _emailConfiguration;
        private readonly ILogger<ActivityHistoryService> _logger;

        public ActivityHistoryService(IActivityHistoryRepository activityHistoryRepository, IAwsEmailService awsEmailService,
            IOptions<UrlPathConfiguration> urlPathConfiguration,
            IOptions<EmailConfiguration> emailConfiguration,
            ILogger<ActivityHistoryService> logger)
        {
            _activityHistoryRepository = activityHistoryRepository;
            _awsEmailService = awsEmailService;
            _urlPathConfiguration = urlPathConfiguration.Value;
            _emailConfiguration = emailConfiguration.Value;
            _logger = logger;
        }

        public async Task<Result> AddActivity(ActivityHistory activity, User user, Deposition deposition)
        {
            try
            {
                activity.ActivityDate = DateTime.UtcNow;
                activity.User = user;
                activity.Deposition = deposition;
                activity.Action = ActivityHistoryAction.JoinDeposition;

                await _activityHistoryRepository.Create(activity);

                var witness = deposition.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
                var startDate = deposition.GetActualStartDate() ?? deposition.StartDate;
                var startDateFormatted = startDate.GetFormattedDateTime(deposition.TimeZone);
                var subject = $"{deposition.Case.Name} - {startDateFormatted}";
                var caseName = $"<b>{deposition.Case.Name}</b>";

                if (!string.IsNullOrEmpty(witness?.Name))
                { 
                    subject = $"{witness.Name} - {deposition.Case.Name} - {startDateFormatted}";
                    caseName = $"<b>{witness.Name}</b> in the case of <b>{caseName}</b>";
                }

                var template = new EmailTemplateInfo
                {
                    EmailTo = new List<string> { user.EmailAddress },
                    TemplateData = new Dictionary<string, string>
                            {
                                { "user-name", user.GetFullName() },
                                { "subject", subject },
                                { "case-name", caseName },
                                { "join-date",  activity.ActivityDate.GetFormattedDateTime(deposition.TimeZone)},
                                { "ip-address", activity.IPAddress },
                                { "device-name", activity.Device },
                                { "browser-name", activity.Browser },
                                { "images-url", $"{_emailConfiguration.ImagesUrl}"},
                                { "logo", $"{_emailConfiguration.ImagesUrl}{_emailConfiguration.LogoImageName}"},
                                { "sign-up-link", $"{_urlPathConfiguration.FrontendBaseUrl}sign-up"},
                            },
                    TemplateName = ApplicationConstants.ActivityTemplateName
                };

                await _awsEmailService.SetTemplateEmailRequest(template, _emailConfiguration.EmailNotification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to add activity");
            }
            
            return Result.Ok();
        }

        public async Task<Result> UpdateUserSystemInfo(Guid depositionId, UserSystemInfo userSystemInfo, User user, string ipAddress)
        {
            try
            {
                var activityHistory = new ActivityHistory() {
                    Action = ActivityHistoryAction.SetSystemInfo,
                    ActionDetails = string.Empty,
                    ActivityDate = DateTime.UtcNow,
                    Browser = userSystemInfo.Browser,
                    CreationDate = DateTime.UtcNow,
                    DepositionId = depositionId,
                    Device = userSystemInfo.Device,
                    OperatingSystem = userSystemInfo.OS,
                    UserId = user.Id,
                    IPAddress = ipAddress
                };

                await _activityHistoryRepository.Create(activityHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to update User system info");
            }
            return Result.Ok();
        }
    }
}
