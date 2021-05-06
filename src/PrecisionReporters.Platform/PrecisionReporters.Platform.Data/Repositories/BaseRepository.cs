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

        public async Task<List<T>> CreateRange(List<T> entities)
        {
            await _dbContext.Set<T>().AddRangeAsync(entities);
            await _dbContext.SaveChangesAsync();
            return entities;
        }


        public async Task<T> Update(T entity)
        {
            var editedEntity = await _dbContext.Set<T>().FirstOrDefaultAsync(e => e.Id == entity.Id);
            editedEntity.CopyFrom(entity);
            await _dbContext.SaveChangesAsync();
            return editedEntity;
        }

        public async Task Remove(T entity)
        {
            _dbContext.Set<T>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveRange(List<T> entities)
        {
            _dbContext.Set<T>().RemoveRange(entities);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<T>> GetByFilter(Expression<Func<T, bool>> filter = null, string[] include = null)
        {
            return await GetByFilter(x => x.CreationDate, SortDirection.Ascend, filter, include);
        }

        public async Task<List<T>> GetByFilter(Expression<Func<T, object>> orderBy,
            SortDirection sortDirection,
            Expression<Func<T, bool>> filter = null,
            string[] include = null)
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

        public async Task<Tuple<int, IEnumerable<T>>> GetByFilterPagination(
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
            string[] include = null,
            int? page = null,
            int? pageSize = null
            )
        {
            IQueryable<T> query = _dbContext.Set<T>();

            if (include != null)
            {
                foreach (var property in include)
                {
                    query = query.Include(property);
                }
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            var totalCount = await query.CountAsync();

            if (page != null)
            {
                query = query.Skip(((int)page - 1) * (int)pageSize);
            }

            if (pageSize != null)
            {
                query = query.Take((int)pageSize);
            }

            var data = await query.ToListAsync();
            return new Tuple<int, IEnumerable<T>>(totalCount, data);
        }

        public async Task<int> GetCountByFilter(Expression<Func<T, bool>> filter)
        {
            IQueryable<T> query = _dbContext.Set<T>();
            query = query.Where(filter);
            return await query.CountAsync();
        }

        public async Task<T> GetFirstOrDefaultByFilter(Expression<Func<T, bool>> filter = null, string[] include = null, bool tracking = true)
        {
            IQueryable<T> query = _dbContext.Set<T>();

            if (!tracking)
                query = query.AsNoTracking();

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

        public async Task<List<T>> GetByFilterOrderByThen(Expression<Func<T, object>> orderBy, SortDirection sortDirection,
            Expression<Func<T, bool>> filter = null, string[] include = null, Expression<Func<T, object>> orderByThen = null)
        {
            IQueryable<T> query = _dbContext.Set<T>();
            IQueryable<T> result;

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

            if (orderByThen != null)
            {
                result = sortDirection == SortDirection.Ascend
                ? query.OrderBy(orderBy).ThenBy(orderByThen)
                : query.OrderByDescending(orderBy).ThenByDescending(orderByThen);
            }
            else
            {
                result = sortDirection == SortDirection.Ascend
                ? query.OrderBy(orderBy)
                : query.OrderByDescending(orderBy);
            }

            return await result.ToListAsync();
        }

        public async Task<T> GetById(Guid id, string[] include = null)
        {
            IQueryable<T> query = _dbContext.Set<T>();

            if (include != null)
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
