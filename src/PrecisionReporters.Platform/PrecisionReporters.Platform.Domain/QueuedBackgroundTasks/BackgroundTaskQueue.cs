using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.QueuedBackgroundTasks
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private ConcurrentQueue<BackgroundTaskDto> _backgroundTaskDto =
        new ConcurrentQueue<BackgroundTaskDto>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void QueueBackgroundWorkItem(
            BackgroundTaskDto backgroundTaskDto)
        {
            if (backgroundTaskDto == null)
            {
                throw new ArgumentNullException(nameof(backgroundTaskDto));
            }

            _backgroundTaskDto.Enqueue(backgroundTaskDto);
            _signal.Release();
        }

        public async Task<BackgroundTaskDto> DequeueAsync(
            CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _backgroundTaskDto.TryDequeue(out var backgroundTaskDto);

            return backgroundTaskDto;
        }
    }
}
