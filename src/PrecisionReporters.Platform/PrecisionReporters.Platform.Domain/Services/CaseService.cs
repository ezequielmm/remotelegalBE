using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class CaseService : ICaseService
    {
        private readonly ICaseRepository _caseRepository;

        public CaseService(ICaseRepository caseRepository)
        {
            _caseRepository = caseRepository;
        }

        public async Task<List<Case>> GetCases()
        {
            return await _caseRepository.GetByFilter();
        }

        public async Task<Case> GetCaseById(Guid id)
        {
            return await _caseRepository.GetById(id);
        }

        public async Task<Case> CreateCase(Case newCase)
        {
            return await _caseRepository.Create(newCase);
        }
    }
}
