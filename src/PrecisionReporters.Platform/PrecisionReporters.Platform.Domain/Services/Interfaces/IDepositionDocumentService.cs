using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IDepositionDocumentService
    {
        Task<Result> CloseStampedDepositionDocument(Document document, DepositionDocument depositionDocument, string identity, string temporalPath);
        Task<Result> CloseDepositionDocument(Document document, Guid depostionId);
        Task<Result<List<DepositionDocument>>> GetEnteredExhibits(Guid depostionId, ExhibitSortField? sortedField = null, SortDirection? sortDirection = null);
        Task<bool> ParticipantCanCloseDocument(Document document, Guid depositionId);
        Task<bool> IsPublicDocument(Guid depositionId, Guid documentId);
        Task<Result> RemoveDepositionTranscript(Guid depositionId, Guid documentId);
    }
}
