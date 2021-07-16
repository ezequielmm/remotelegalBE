using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ITranscriptionLiveService : IDisposable
    {
        Task InitializeRecognition(string userEmail, string depositionId, int sampleRate);
        Task RecognizeAsync(byte[] audioChunk);
        void StopTranscriptStream();
    }
}
