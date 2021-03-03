using FluentResults;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IDraftTranscriptGeneratorService
    {
        Task<Result> GenerateDraftTranscriptionPDF(DraftTranscriptDto draftTranscriptDto); 
        Task<Result> SaveDraftTranscriptionPDF(DraftTranscriptDto draftTranscriptDto);
    }
}
