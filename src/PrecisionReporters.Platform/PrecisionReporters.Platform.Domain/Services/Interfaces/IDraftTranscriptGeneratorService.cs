using FluentResults;
using PrecisionReporters.Platform.Domain.Dtos;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IDraftTranscriptGeneratorService
    {
        Task<Result> GenerateDraftTranscription(DraftTranscriptDto draftTranscriptDto);
    }
}
