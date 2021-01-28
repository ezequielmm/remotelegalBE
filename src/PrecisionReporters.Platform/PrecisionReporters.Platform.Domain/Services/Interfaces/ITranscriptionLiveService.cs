using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ITranscriptionLiveService
    {
        Task<Transcription> RecognizeAsync(byte[] audioChunk, string userEmail, string depositionId, int sampleRate);
    }
}
