using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Transcript.Api.Utils.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Transcript.Api.Utils
{
    public class SignalRTranscriptionFactory : ISignalRTranscriptionFactory
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _namedMutex = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly ConcurrentDictionary<string, ITranscriptionLiveService> _transcriptionLiveServices = new ConcurrentDictionary<string, ITranscriptionLiveService>();

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger _logger;

        public SignalRTranscriptionFactory(IServiceScopeFactory serviceScopeFactory, ILogger<SignalRTranscriptionFactory> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task InitializeRecognitionAsync(string connectionId, string userEmail, string depositionId, int sampleRate)
        {
            var semaphoreSlim = _namedMutex.GetOrAdd(connectionId, (key) => new SemaphoreSlim(1, 1));
            try
            {
                await semaphoreSlim.WaitAsync();
                if (_transcriptionLiveServices.ContainsKey(connectionId))
                {
                    _logger.LogDebug($"A {nameof(ITranscriptionLiveService)} for {{ConnectionId}} was found. Recognition was already initialized.", connectionId);
                    return;
                }

                _logger.LogInformation($"Creating {nameof(ITranscriptionLiveService)} and initializing recognition for user {{UserEmail}} on deposition {{DepositionId}} with sample rate {{SampleRate}}.", userEmail, depositionId, sampleRate);
                var transcriptionService = await CreateAndStartTranscriptionServiceAsync(userEmail, depositionId, sampleRate);
                _transcriptionLiveServices.TryAdd(connectionId, transcriptionService);
                _logger.LogDebug("Recognition started for {ConnectionId}.", connectionId);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public bool TryGetTranscriptionLiveService(string connectionId, out ITranscriptionLiveService transcriptionLiveService)
            => _transcriptionLiveServices.TryGetValue(connectionId, out transcriptionLiveService);

        public async Task UnsubscribeAsync(string connectionId)
        {
            _logger.LogDebug("Attempting to stop recognition for {ConnectionId}.", connectionId);
            if (!_transcriptionLiveServices.TryRemove(connectionId, out var transcriptionService))
            {
                _logger.LogDebug("Recognition for {ConnectionId} was already stopped.", connectionId);
                return;
            }

            try
            {
                using (transcriptionService)
                {
                    await transcriptionService.StopRecognitionAsync();
                }
            }
            finally
            {
                if (_namedMutex.TryRemove(connectionId, out var semaphoreSlim))
                {
                    semaphoreSlim.Dispose();
                }
            }
        }

        private async Task<ITranscriptionLiveService> CreateAndStartTranscriptionServiceAsync(string userEmail, string depositionId, int sampleRate)
        {

            var scope = _serviceScopeFactory.CreateScope();
            var transcriptionService = scope.ServiceProvider.GetRequiredService<ITranscriptionLiveService>();
            try
            {
                await transcriptionService.StartRecognitionAsync(userEmail, depositionId, sampleRate);
            }
            catch (Exception)
            {
                transcriptionService.Dispose();
                throw;
            }

            return transcriptionService;
        }
    }
}