using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class CaseService : ICaseService
    {
        private const SortDirection DEFAULT_SORT_DIRECTION = SortDirection.Ascend;
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
            newCase.Members = new List<Member> { new Member { User = user } };
            return await _caseRepository.Create(newCase);
        }

        public async Task<List<Case>> GetCasesForUser(string userEmail, CaseSortField? sortedField = null, SortDirection? sortDirection = null)
        {
            var user = await _userService.GetUserByEmail(userEmail);
            var includes = new [] { $"{nameof(Case.AddedBy)}", $"{nameof(Case.Members)}" };

            // TODO: Move this to BaseRepository and find a generic way to apply OrderBy
            Expression<Func<Case, object>> orderBy = sortedField switch
            {
                CaseSortField.AddedBy => x => x.AddedBy,
                CaseSortField.CaseNumber => x => x.CaseNumber,
                CaseSortField.CreatedDate => x => x.CreationDate,
                _ => x => x.Name,
            };
            return await _caseRepository.GetByFilter(
                orderBy,
                sortDirection ?? DEFAULT_SORT_DIRECTION,
                x => x.Members.Any(m => m.UserId == user.Id),
                includes);
        }
    }
}
