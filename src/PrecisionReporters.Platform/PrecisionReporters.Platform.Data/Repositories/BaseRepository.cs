using Microsoft.EntityFrameworkCore;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Data.Repositories
{
    public class BaseRepository<T> : IRepository<T>
        where T : BaseEntity<T>
    {
        public readonly ApplicationDbContext _dbContext;

        public BaseRepository(ApplicationDbContext dbcontext) => _dbContext = dbcontext;

        public async Task<T> Create(T entity)
        {
            await _dbContext.Set<T>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<T> Update(T entity)
        {
            var editedEntity = await _dbContext.Set<T>().FirstOrDefaultAsync(e => e.Id == entity.Id);
            editedEntity.CopyFrom(entity);
            await _dbContext.SaveChangesAsync();
            return editedEntity;
        }

        public async Task<List<T>> GetByFilter(Expression<Func<T, bool>> filter = null, string[] include = null)
        {
            return await GetByFilter(x => x.CreationDate, SortDirection.Ascend, filter, include);
        }

        public async Task<List<T>> GetByFilter(Expression<Func<T, object>> orderBy, SortDirection sortDirection, Expression<Func<T, bool>> filter = null, string[] include = null)
        {
            IQueryable<T> query = _dbContext.Set<T>();

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

            var result = sortDirection == SortDirection.Ascend
                ? query.OrderBy(orderBy)
                : query.OrderByDescending(orderBy);

            return await result.ToListAsync();
        }

        public async Task<T> GetFirstOrDefaultByFilter(Expression<Func<T, bool>> filter = null, string[] include = null)
        {
            IQueryable<T> query = _dbContext.Set<T>();

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

            return await query.FirstOrDefaultAsync();
        }

        public async Task<T> GetById(Guid id, string[] include = null)
        {
            IQueryable<T> query = _dbContext.Set<T>();

            if (include!=null)
            {
                foreach (var property in include)
                {
                    query = query.Include(property);
                }
            }

            return await query.FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
