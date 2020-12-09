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
            Expression<Func<Deposition, bool>> filter = null, string[] include = null)
        {
            IQueryable<Deposition> query = _dbContext.Set<Deposition>();

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

            var result = sortDirection == SortDirection.Ascend
                ? query.OrderBy(orderBy).ThenBy(orderByDefault)
                : query.OrderByDescending(orderBy).ThenBy(orderByDefault);

            return await result.ToListAsync();
        }
    }
}
