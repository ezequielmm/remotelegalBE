using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ITranscriptionLiveService
    {
        Task InitializeRecognition(string userEmail, string depositionId, int sampleRate);
        Task RecognizeAsync(byte[] audioChunk);
        void StopTranscriptStream();
    }

    public class TranscriptionEventArgs : EventArgs
    {
        public Transcription Transcription { get; set; }
    }
}
