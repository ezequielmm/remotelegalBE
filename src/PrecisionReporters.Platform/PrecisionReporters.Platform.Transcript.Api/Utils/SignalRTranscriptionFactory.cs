using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Transcript.Api.Utils.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Transcript.Api.Utils
{
    public class SignalRTranscriptionFactory : ISignalRTranscriptionFactory
    {
        private readonly ConcurrentDictionary<string, ConnectionResources> _connectionsResources = new ConcurrentDictionary<string, ConnectionResources>();

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger _logger;

        public SignalRTranscriptionFactory(IServiceScopeFactory serviceScopeFactory, ILogger<SignalRTranscriptionFactory> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task InitializeRecognitionAsync(string connectionId, string userEmail, string depositionId, int sampleRate)
        {
            var connectionResources = _connectionsResources.GetOrAdd(connectionId, (key) => new ConnectionResources());
            var semaphoreSlim = connectionResources.SemaphoreSlim;
            try
            {
                await semaphoreSlim.WaitAsync();
                var transcriptionServiceAlreadyInitialized = connectionResources.ServiceScopeContainer != null;
                if (transcriptionServiceAlreadyInitialized)
                {
                    _logger.LogInformation($"A {nameof(ITranscriptionLiveService)} for {{ConnectionId}} was found. Recognition was already initialized.", connectionId);
                    return;
                }

                _logger.LogInformation($"Creating {nameof(ITranscriptionLiveService)} and initializing recognition for user {{UserEmail}} on deposition {{DepositionId}} with sample rate {{SampleRate}}.", userEmail, depositionId, sampleRate);
                connectionResources.ServiceScopeContainer = await CreateAndStartTranscriptionServiceAsync(userEmail, depositionId, sampleRate);
                _logger.LogInformation("Recognition started for {ConnectionId}.", connectionId);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public bool TryGetTranscriptionLiveService(string connectionId, out ITranscriptionLiveService transcriptionLiveService)
        {
            if (!_connectionsResources.TryGetValue(connectionId, out var connectionResources))
            {
                transcriptionLiveService = default;
                return false;
            }

            transcriptionLiveService = connectionResources.ServiceScopeContainer.Service;
            return true;
        }

        public void Unsubscribe(string connectionId)
        {
            _logger.LogInformation("Attempting to stop recognition for {ConnectionId}.", connectionId);
            if (!_connectionsResources.TryRemove(connectionId, out var connectionResources))
            {
                _logger.LogInformation("Recognition for {ConnectionId} was already stopped.", connectionId);
                return;
            }

            Task.Run(async () =>
            {
                // Both stopping and disposing TranscriptionLiveAzureService takes too long, so we are using fire and forget.
                try
                {
                    using (connectionResources)
                    {
                        var transcriptionLiveService = connectionResources.ServiceScopeContainer.Service;
                        await transcriptionLiveService.StopRecognitionAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An exception occur while trying stop and dispose {nameof(ITranscriptionLiveService)} for {{ConnectionId}}", connectionId);
                }
            });
        }

        private async Task<ServiceScopeContainer<ITranscriptionLiveService>> CreateAndStartTranscriptionServiceAsync(string userEmail, string depositionId, int sampleRate)
        {
            var scope = _serviceScopeFactory.CreateScope();
            var transcriptionLiveService = scope.ServiceProvider.GetRequiredService<ITranscriptionLiveService>();
            try
            {
                await transcriptionLiveService.StartRecognitionAsync(userEmail, depositionId, sampleRate);
            }
            catch (Exception)
            {
                scope.Dispose();
                throw;
            }

            var serviceScopeContainer = new ServiceScopeContainer<ITranscriptionLiveService>
            {
                ServiceScope = scope,
                Service = transcriptionLiveService
            };
            return serviceScopeContainer;
        }
    }
}