using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Data.Enums;
using FluentResults;
using PrecisionReporters.Platform.Domain.Errors;

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

        public async Task<Result<Case>> GetCaseById(Guid id)
        {
            var foundCase = await _caseRepository.GetById(id, nameof(Case.AddedBy));
            if (foundCase == null)
                return Result.Fail(new ResourceNotFoundError($"Case with id {id} not found."));

            return Result.Ok(foundCase);
        }

        public async Task<Result<Case>> CreateCase(string userEmail, Case newCase)
        {
            var user = await _userService.GetUserByEmail(userEmail);
            if (user == null)
                return Result.Fail(new ResourceNotFoundError($"User with email {userEmail} not found."));

            newCase.AddedBy = user;
            newCase.Members = new List<Member> { new Member { User = user } };

            var createdCase = await _caseRepository.Create(newCase);
            return Result.Ok(createdCase);
        }

        public async Task<Result<List<Case>>> GetCasesForUser(string userEmail, CaseSortField? sortedField = null, SortDirection? sortDirection = null)
        {
            var user = await _userService.GetUserByEmail(userEmail);
            if (user == null)
                return Result.Fail(new ResourceNotFoundError($"User with email {userEmail} not found"));

            var includes = new string[] { nameof(Case.AddedBy), nameof(Case.Members) };

            // TODO: Move this to BaseRepository and find a generic way to apply OrderBy
            Expression<Func<Case, object>> orderBy = sortedField switch
            {
                CaseSortField.AddedBy => x => x.AddedBy,
                CaseSortField.CaseNumber => x => x.CaseNumber,
                CaseSortField.CreatedDate => x => x.CreationDate,
                _ => x => x.Name,
            };
            var foundCases = await _caseRepository.GetByFilter(
                orderBy,
                sortDirection ?? DEFAULT_SORT_DIRECTION,
                x => x.Members.Any(m => m.UserId == user.Id),
                includes);

            // note: empty list is ok, null is an error
            if (foundCases == null)
                return Result.Fail(new ResourceNotFoundError()); // TODO: What would cause foundCases to be null? Use the right type of error.

            return Result.Ok(foundCases);
        }
    }
}
