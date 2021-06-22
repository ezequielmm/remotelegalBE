using PrecisionReporters.Platform.Domain.Dtos;
using System.Threading;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces
{
    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItem(BackgroundTaskDto backgroundTaskDto);
        Task<BackgroundTaskDto> DequeueAsync(CancellationToken cancellationToken);

    }
}
