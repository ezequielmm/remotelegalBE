using FluentResults;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IDraftTranscriptGeneratorService
    {
        Task<Result> GenerateDraftTranscriptionPDF(Guid depositionId);
    }
}
