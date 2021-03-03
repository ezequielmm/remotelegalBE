﻿using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Commons;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IDepositionDocumentService
    {
        Task<Result> CloseStampedDepositionDocument(Document document, DepositionDocument depositionDocument, string identity, FileTransferInfo file);
        Task<Result> CloseDepositionDocument(Document document, Guid depostionId);
        Task<Result<List<DepositionDocument>>> GetEnteredExhibits(Guid depostionId, ExhibitSortField? sortedField = null, SortDirection? sortDirection = null);
        Task<bool> ParticipantCanCloseDocument(Document document, Guid depositionId);
        Task<bool> IsPublicDocument(Guid depositionId, Guid documentId);
    }
}
