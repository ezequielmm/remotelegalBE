using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Commons;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ICaseService
    {
        Task<List<Case>> GetCases(Expression<Func<Case, bool>> filter = null, string[] include = null);
        Task<Result<Case>> GetCaseById(Guid id, string[] include = null);
        Task<Result<Case>> CreateCase(string userEmail, Case newCase);
        Task<Result<List<Case>>> GetCasesForUser(string userEmail, CaseSortField? sortedField = null, SortDirection? sortDirection = null);
        Task<Result<Case>> ScheduleDepositions(string userEmail, Guid caseId, IEnumerable<Deposition> depositions, Dictionary<string, FileTransferInfo> files);
    }
}
