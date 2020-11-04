﻿using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Data.Repositories.Interfaces
{
    public interface IRepository<T> where T : BaseEntity<T>
    {
        Task<T> Create(T entity);
        Task<T> Update(T entity);
        Task<T> GetFirstOrDefaultByFilter(Expression<Func<T, bool>> filter = null, string include = "");
        Task<List<T>> GetByFilter(Expression<Func<T, bool>> filter = null, string[] include = null);
        Task<T> GetById(Guid id, string include = "");
    }
}
