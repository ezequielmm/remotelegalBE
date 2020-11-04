using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class CaseService : ICaseService
    {
        private readonly ICaseRepository _caseRepository;
        private readonly IUserService _userService;

        public CaseService(ICaseRepository caseRepository, IUserService userService)
        {
            _caseRepository = caseRepository;
            _userService = userService;
        }

        public async Task<List<Case>> GetCases(Expression<Func<Case, bool>> filter = null, string[] include = null)
        {
            return await _caseRepository.GetByFilter(filter, include);
        }

        public async Task<Case> GetCaseById(Guid id)
        {
            return await _caseRepository.GetById(id, nameof(Case.AddedBy));
        }

        public async Task<Case> CreateCase(string userEmail, Case newCase)
        {
            var user = await _userService.GetUserByEmail(userEmail);
            newCase.AddedBy = user;
            return await _caseRepository.Create(newCase);
        }

        public async Task<List<Case>> GetCasesForUser(string userEmail)
        {
            var user = await _userService.GetUserByEmail(userEmail);
            var includes = new string[] { $"{nameof(Case.AddedBy)}", $"{nameof(Case.Members)}" };
            var result = await _caseRepository.GetByFilter(x => x.Members.Any(m => m.UserId == user.Id), includes);
            return result;
        }
    }
}
