﻿using Microsoft.EntityFrameworkCore;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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

        public async Task<List<T>> GetByFilter(Expression<Func<T, bool>> filter = null)
        {
            if (filter != null)
                return await _dbContext.Set<T>().Where(filter).ToListAsync();
            return await _dbContext.Set<T>().ToListAsync();
        }

        public async Task<T> GetById(Guid id)
        {
            return await _dbContext.Set<T>().FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
