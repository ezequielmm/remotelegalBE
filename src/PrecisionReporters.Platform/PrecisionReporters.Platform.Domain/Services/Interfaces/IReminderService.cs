using FluentResults;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IReminderService
    {
        Task<Result<bool>> SendDailyReminder();
        Task<Result<bool>> SendReminder();
    }
}
