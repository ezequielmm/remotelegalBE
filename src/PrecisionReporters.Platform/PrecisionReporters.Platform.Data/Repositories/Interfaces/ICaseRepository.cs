using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public interface ICaseRepository
    {
        Task<List<Case>> GetCases();
        Task<Case> GetCaseById(Guid id);
        Task<Case> CreateCase(Case newCase);
    }
}
