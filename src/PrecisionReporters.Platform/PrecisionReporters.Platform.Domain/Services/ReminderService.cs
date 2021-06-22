using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Configurations;
using PrecisionReporters.Platform.Domain.Enums;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class ReminderService : IReminderService
    {
        private readonly IDepositionRepository _depositionRepository;
        private readonly IDepositionEmailService _depositionEmailService;
        private readonly ITransactionHandler _transactionHandler;
        private readonly ILogger<ReminderService> _logger;
        private readonly ReminderConfiguration _reminderConfiguration;
        public ReminderService(
            IDepositionRepository depositionRepository,
            IDepositionEmailService depositionEmailService,
            ITransactionHandler transactionHandler,
            ILogger<ReminderService> logger, IOptions<ReminderConfiguration> reminderConfiguration)
        {
            _depositionRepository = depositionRepository;
            _depositionEmailService = depositionEmailService;
            _transactionHandler = transactionHandler;
            _logger = logger;
            _reminderConfiguration = reminderConfiguration.Value;
        }

        public async Task<Result<bool>> SendReminder()
        {
            var minutesBefore = _reminderConfiguration.MinutesBefore;
            var reminderRecurrency = _reminderConfiguration.ReminderRecurrency;
            var tasks = new List<Task>();
            var currentDate = DateTime.UtcNow;
            try
            {
                var transactionResult = await _transactionHandler.RunAsync<bool>(async () =>
                {
                    _logger.LogInformation($"Init reminder method transaction");
                    foreach (var item in minutesBefore)
                    {
                        var startDate = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, currentDate.Hour, currentDate.Minute, 0, 0, DateTimeKind.Utc).AddMinutes(item);
                        var endDate = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, currentDate.Hour, currentDate.Minute, 59, 0, DateTimeKind.Utc).AddMinutes(item + reminderRecurrency - 1);
                        var includes = new[] { nameof(Deposition.Case), nameof(Deposition.Participants) };

                        var depositions = await _depositionRepository.GetByFilter(d => d.StartDate >= startDate && d.StartDate <= endDate && d.Status == DepositionStatus.Confirmed, includes);

                        depositions.ForEach(d =>
                        tasks.AddRange(d.Participants.Where(p => !string.IsNullOrWhiteSpace(p.Email)).Select(participant => _depositionEmailService.SendDepositionReminder(d, participant))));
                    }
                    await Task.WhenAll(tasks);
                    _logger.LogInformation($"Finish reminder method transaction, reminders sent");
                    return Result.Ok(true);
                });
                return transactionResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to send reminders");
                return Result.Fail(new ExceptionalError("Unable to send reminders", ex));
            }
        }

        public async Task<Result<bool>> SendDailyReminder()
        {
            try
            {
                var transactionResult = await _transactionHandler.RunAsync<bool>(async () =>
                {
                    _logger.LogInformation($"Init daily reminder method transaction");
                    var currentDate = DateTime.UtcNow.AddDays(1);
                    var timeZone = GetTimeZones();
                    var includes = new[] { nameof(Deposition.Case), nameof(Deposition.Participants) };
                    List<Deposition> depositions = new List<Deposition>();
                    foreach (var zone in timeZone)
                    {
                        var startDate = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 0, 0, 0, 0).GetWithSpecificTimeZone(zone);
                        var endDate = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 23, 59, 59, 59).GetWithSpecificTimeZone(zone);
                        var timeZoneDepositions = await _depositionRepository.GetByFilter(d => d.StartDate >= startDate.ToUniversalTime()
                        && d.StartDate <= endDate.ToUniversalTime()
                        && d.Status == DepositionStatus.Confirmed && timeZone.Contains(d.TimeZone), includes);
                        depositions.AddRange(timeZoneDepositions);
                    }
                    var tasks = new List<Task>();

                    depositions.ForEach(d =>
                    tasks.AddRange(d.Participants.Where(p => !string.IsNullOrWhiteSpace(p.Email)).Select(participant => _depositionEmailService.SendDepositionReminder(d, participant))));

                    await Task.WhenAll(tasks);
                    _logger.LogInformation($"Finish daily reminder method transaction, reminders sent");
                    return Result.Ok(true);
                });
                return transactionResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to send reminders");
                return Result.Fail(new ExceptionalError("Unable to send reminders", ex));
            }

        }

        private List<string> GetTimeZones()
        {
            var dailyExecutionTs = TimeSpan.Parse(_reminderConfiguration.DailyExecution);
            var utcDate = DateTime.UtcNow;
            var date = new DateTime(utcDate.Year, utcDate.Month, utcDate.Day, utcDate.Hour, 0, 0, 0, DateTimeKind.Utc);
            var lstTimeZone = new List<string>();
            foreach (var timeZone in Enum.GetNames(typeof(USTimeZone)))
            {
                USTimeZone enumValue = (USTimeZone)Enum.Parse(typeof(USTimeZone), timeZone);
                var dateTimeZone = date.GetConvertedTime(enumValue.GetDescription());
                var executionTime = new DateTime(dateTimeZone.Year, dateTimeZone.Month, dateTimeZone.Day, dailyExecutionTs.Hours, dailyExecutionTs.Minutes, 0, 0);
                if ((dateTimeZone - executionTime).TotalMinutes >= 0 && (dateTimeZone - executionTime).TotalMinutes < 60)
                    lstTimeZone.Add(enumValue.GetDescription());
            }
            return lstTimeZone;
        }
    }
}
