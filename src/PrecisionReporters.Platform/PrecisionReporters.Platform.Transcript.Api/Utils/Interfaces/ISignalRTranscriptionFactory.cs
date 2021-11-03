using System.Threading.Tasks;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

namespace PrecisionReporters.Platform.Transcript.Api.Utils.Interfaces
{
    public interface ISignalRTranscriptionFactory
    {
        Task InitializeRecognitionAsync(string connectionId, string userEmail, string depositionId, int sampleRate);
        bool TryGetTranscriptionLiveService(string connectionId, out ITranscriptionLiveService transcriptionLiveService);
        void Unsubscribe(string connectionId);
    }
}