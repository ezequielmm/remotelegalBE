using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Transcript.Api.Utils.Interfaces;

namespace PrecisionReporters.Platform.Transcript.Api.Utils
{
    public class SignalRTranscriptionFactory : ISignalRTranscriptionFactory
    {
        private static readonly ConcurrentDictionary<string, ITranscriptionLiveService> _multitonSignalRTranscriptions =
            new ConcurrentDictionary<string, ITranscriptionLiveService>();

        private readonly IServiceScopeFactory _serviceScopeFactory;

        public SignalRTranscriptionFactory(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task TryInitializeRecognition(string connectionId, string userEmail, string depositionId, int sampleRate)
        {
           if (!_multitonSignalRTranscriptions.ContainsKey(connectionId))
           {
               var scope = _serviceScopeFactory.CreateScope();
               var transcriptionLiveService = scope.ServiceProvider.GetService<ITranscriptionLiveService>();
               if (_multitonSignalRTranscriptions.TryAdd(connectionId, transcriptionLiveService))
               {
                   await _multitonSignalRTranscriptions[connectionId].InitializeRecognition(userEmail, depositionId, sampleRate);
               }
           }
        }

        public ITranscriptionLiveService GetTranscriptionLiveService(string connectionId)
        {
            _multitonSignalRTranscriptions.TryGetValue(connectionId, out var transcriptionLiveService);
            return transcriptionLiveService;
        }

        public void Unsubscribe(string connectionId)
        {
            if (_multitonSignalRTranscriptions.TryRemove(connectionId, out var signalRTranscriptionLiveService))
            {
                using (signalRTranscriptionLiveService)
                { 
                    signalRTranscriptionLiveService.StopTranscriptStream();
                }
            }
        }
    }
}