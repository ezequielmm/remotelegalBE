using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IDepositionEmailService
    {
        Task SendJoinDepositionEmailNotification(Deposition deposition);
        Task SendJoinDepositionEmailNotification(Deposition deposition, Participant participant);
        Task SendCancelDepositionEmailNotification(Deposition deposition, Participant participant);
        Task SendReSheduleDepositionEmailNotification(Deposition deposition, Participant participant, DateTime oldStartDate, string oldTimeZone);
        Task SendDepositionReminder(Deposition deposition, Participant participant);
    }
}
