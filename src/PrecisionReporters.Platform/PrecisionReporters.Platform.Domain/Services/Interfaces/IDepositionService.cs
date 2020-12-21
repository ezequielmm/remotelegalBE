using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IDepositionService
    {
        Task<List<Deposition>> GetDepositions(Expression<Func<Deposition, bool>> filter = null, string[] include = null);
        Task<Result<Deposition>> GetDepositionById(Guid id);
        Task<Result<Deposition>> GetDepositionByIdWithDocumentUsers(Guid id);
        Task<Result<Deposition>> GenerateScheduledDeposition(Deposition deposition, List<Document> uploadedDocuments);
        Task<List<Deposition>> GetDepositionsByStatus(DepositionStatus? status, DepositionSortField? sortedField, SortDirection? sortDirection, string userEmail);
        Task<Result<JoinDepositionDto>> JoinDeposition(Guid id, string identity);
        Task<Result<Deposition>> EndDeposition(Guid id);
        Task<Result<Deposition>> AddDepositionEvent(Guid id, DepositionEvent depositionEvent, string userEmail);
        Task<Result<Deposition>> GoOnTheRecord(Guid id, bool onRecord, string userEmail);
    }
}