using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Data.Repositories.Interfaces
{
    public interface IRepository<T> where T : BaseEntity<T>
    {
        Task<T> Create(T entity);
        Task<List<T>> CreateRange(List<T> entities);
        Task<T> Update(T entity);
        Task Remove(T entity);
        Task RemoveRange(List<T> entities);
        Task<T> GetFirstOrDefaultByFilter(Expression<Func<T, bool>> filter = null, string[] include = null, bool tracking = true);
        Task<List<T>> GetByFilter(Expression<Func<T, bool>> filter = null, string[] include = null);
        Task<List<T>> GetByFilter(Expression<Func<T, object>> orderBy, SortDirection sortDirection, Expression<Func<T, bool>> filter = null, string[] include = null);
        Task<Tuple<int, IEnumerable<T>>> GetByFilterPagination(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string[] include = null, int? page = null, int? pageSize = null);
        Task<Tuple<int, IQueryable<T>>> GetByFilterPaginationQueryable(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, string[] include = null, int? page = null, int? pageSize = null);
        Task<int> GetCountByFilter(Expression<Func<T, bool>> filter);
        Task<List<T>> GetByFilterOrderByThen(Expression<Func<T, object>> orderBy, SortDirection sortDirection, Expression<Func<T, bool>> filter = null, string[] include = null, Expression<Func<T, object>> orderByThen = null);
        Task<T> GetById(Guid id, string[] include = null);
    }
}
