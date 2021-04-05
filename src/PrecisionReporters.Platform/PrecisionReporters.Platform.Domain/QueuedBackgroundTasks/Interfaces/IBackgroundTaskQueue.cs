using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces
{
    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItem(BackgroundTaskDto workItem);
        Task<BackgroundTaskDto> DequeueAsync(CancellationToken cancellationToken);

    }
}
