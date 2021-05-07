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
            IQueryable<Deposition> query = _dbContext.Set<Deposition>();
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

        public async Task<Deposition> GetByIdWithAdmittedParticipants(Guid id, string[] include = null)
        {
            IQueryable<Deposition> depositions = _dbContext.Set<Deposition>();
            IQueryable<Participant> participants = _dbContext.Set<Participant>();

            if (include != null)
            {
                foreach (var property in include)
                {
                    depositions = depositions.Include(property);
                }
            }

            var result = await GetDepositionWithAdmittedParticipant(depositions);

            return result.FirstOrDefault(x => x.Id == id);
        }

        public async Task<List<Deposition>> GetDepositionWithAdmittedParticipant(IQueryable<Deposition> depositions)
        {
            IQueryable<Participant> participants = _dbContext.Set<Participant>();

            var result = from d in depositions
                         join p in participants.Where(x => x.IsAdmitted == true)
                              on d.Id equals p.DepositionId
                         select new { d, p };

            var filterParticipant = result.Select(x => x.p).ToList();

            foreach (var item in depositions)
            {
                item.Participants = filterParticipant.Where(p => p.DepositionId == item.Id).ToList();
            }

            return await depositions.ToListAsync();
        }
    }
}