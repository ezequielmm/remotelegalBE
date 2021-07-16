using System.Threading.Tasks;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

namespace PrecisionReporters.Platform.Transcript.Api.Utils.Interfaces
{
    public interface ISignalRTranscriptionFactory
    {
        Task TryInitializeRecognition(string connectionId, string userEmail, string depositionId, int sampleRate);
        ITranscriptionLiveService GetTranscriptionLiveService(string connectionId);
        void Unsubscribe(string connectionId);
    }
}