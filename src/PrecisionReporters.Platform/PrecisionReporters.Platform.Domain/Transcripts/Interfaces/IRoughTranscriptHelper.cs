using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Transcripts.Interfaces
{
    public interface IRoughTranscriptHelper
    {
        int CalculatePagesToAdd(int transcripts);
        List<string> SplitSentences(string sentence);
        List<string> AddBlankRowsToList(List<string> transcripts);
        List<string> CreateTranscriptRows(List<Transcription> transcripts, bool isPDF);
        Task<Result> SaveFinalDraftOnS3(Stream streamDoc, Deposition deposition, string fileName);
        Task<Result> SaveDraftTranscription(DraftTranscriptDto draftTranscriptDto, string fileName, string fileType, DocumentType documentType);
    }
}
