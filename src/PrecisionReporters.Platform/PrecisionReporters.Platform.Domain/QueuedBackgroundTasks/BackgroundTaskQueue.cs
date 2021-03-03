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
        private ConcurrentQueue<DraftTranscriptDto> _draftTranscriptDto =
        new ConcurrentQueue<DraftTranscriptDto>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void QueueBackgroundWorkItem(
            DraftTranscriptDto draftTranscriptDto)
        {
            if (draftTranscriptDto == null)
            {
                throw new ArgumentNullException(nameof(draftTranscriptDto));
            }

            _draftTranscriptDto.Enqueue(draftTranscriptDto);
            _signal.Release();
        }

        public async Task<DraftTranscriptDto> DequeueAsync(
            CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _draftTranscriptDto.TryDequeue(out var draftTranscriptDto);

            return draftTranscriptDto;
        }
    }
}
