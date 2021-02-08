using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Data.Repositories.Interfaces
{
    public interface IRepository<T> where T : BaseEntity<T>
    {
        Task<T> Create(T entity);
        Task<List<T>> CreateRange(List<T> entities);
        Task<T> Update(T entity);
        Task Remove(T entity);
        Task RemoveRange(List<T> entities);
        Task<T> GetFirstOrDefaultByFilter(Expression<Func<T, bool>> filter = null, string[] include = null);
        Task<List<T>> GetByFilter(Expression<Func<T, bool>> filter = null, string[] include = null);
        Task<List<T>> GetByFilter(Expression<Func<T, object>> orderBy, SortDirection sortDirection, Expression<Func<T, bool>> filter = null, string[] include = null);
        Task<List<T>> GetByFilterOrderByThen(Expression<Func<T, object>> orderBy, SortDirection sortDirection, Expression<Func<T, bool>> filter = null, string[] include = null, Expression<Func<T, object>> orderByThen = null);
        Task<T> GetById(Guid id, string[] include = null);
    }
}
