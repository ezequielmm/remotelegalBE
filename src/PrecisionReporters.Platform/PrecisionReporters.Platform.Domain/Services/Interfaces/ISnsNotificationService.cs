using FluentResults;
using System.IO;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ISnsNotificationService
    {
        Task<Result<string>> Notify(Stream message);
    }
}
