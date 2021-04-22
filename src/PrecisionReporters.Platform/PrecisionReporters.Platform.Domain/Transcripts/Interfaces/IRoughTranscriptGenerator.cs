using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Transcripts.Interfaces
{
    public interface IRoughTranscriptGenerator
    {
        Task<Result> GenerateTranscriptTemplate(DraftTranscriptDto draftTranscriptDto, Deposition deposition, List<Transcription> transcripts);
    }
}