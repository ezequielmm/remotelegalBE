using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class DepositionRepository : BaseRepository<Deposition>, IDepositionRepository
    {
        public DepositionRepository(ApplicationDbContext dbcontext) : base(dbcontext)
        {
        }

        public async Task<List<Deposition>> GetByStatus(Expression<Func<Deposition, object>> orderBy, SortDirection sortDirection,
            Expression<Func<Deposition, bool>> filter = null, string[] include = null, Expression<Func<Deposition, object>> orderByThen = null)
        {
            IQueryable<Deposition> query = _dbContext.Set<Deposition>().AsNoTracking();
            IQueryable<Deposition> result;

            if (include != null)
            {
                foreach (var property in include)
                {
                    query = query.Include(property);
                }
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            Expression<Func<Deposition, object>> orderByDefault = x => x.StartDate;

            if (orderByThen != null)
            {
                result = sortDirection == SortDirection.Ascend
                ? query.OrderBy(orderBy).ThenBy(orderByThen).ThenBy(orderByDefault)
                : query.OrderByDescending(orderBy).ThenByDescending(orderByThen).ThenBy(orderByDefault);
            }
            else
            {
                result = sortDirection == SortDirection.Ascend
                ? query.OrderBy(orderBy).ThenBy(orderByDefault)
                : query.OrderByDescending(orderBy).ThenBy(orderByDefault);
            }

            return await result.ToListAsync();
        }

        public Task<List<Deposition>> GetDepositionWithAdmittedParticipant(IQueryable<Deposition> depositions)
        {
            var deposWithParticipants = depositions
                .Where(d => d.Participants.Any(p => p.IsAdmitted.Value && p.DepositionId == d.Id))
                .Include(d => d.Participants)
                .AsNoTracking()
                .ToListAsync();

            return deposWithParticipants;
        }
    }
}