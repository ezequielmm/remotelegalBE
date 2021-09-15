using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ITranscriptionLiveService : IDisposable
    {
        Task StartRecognitionAsync(string userEmail, string depositionId, int sampleRate);
        bool TryAddAudioChunkToBuffer(byte[] audioChunk);
        Task StopRecognitionAsync();
    }
}
