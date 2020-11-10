using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface ICaseService
    {
        Task<List<Case>> GetCases(Expression<Func<Case, bool>> filter = null, string[] include = null);
        Task<Case> GetCaseById(Guid id);
        Task<Case> CreateCase(string userEmail, Case newCase);
        Task<List<Case>> GetCasesForUser(string userEmail, CaseSortField? sortedField = null, SortDirection? sortDirection = null);    
    }
}
