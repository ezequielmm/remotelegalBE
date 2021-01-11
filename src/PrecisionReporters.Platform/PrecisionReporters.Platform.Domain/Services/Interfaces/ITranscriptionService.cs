using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ITranscriptionService
    {
        Task<Transcription> RecognizeAsync(byte[] audioChunk, string userEmail, string depositionId);
        Task<Result<List<Transcription>>> GetTranscriptionsByDepositionId(Guid depositionId);
    }
}
