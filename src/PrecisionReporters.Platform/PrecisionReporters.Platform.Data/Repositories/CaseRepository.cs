using Microsoft.EntityFrameworkCore;
using PrecisionReporters.Platform.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Data.Repositories
{
    // TODO: Create generic repository implementation
    public class CaseRepository : ICaseRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public CaseRepository(ApplicationDbContext dbcontext)
        {
            _dbContext = dbcontext;
        }

        public async Task<List<Case>> GetCases()
        {
            return await _dbContext.Cases.ToListAsync();
        }

        public async Task<Case> GetCaseById(Guid id)
        {
            return await _dbContext.Cases.FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Case> CreateCase(Case newCase)
        {
            await _dbContext.Cases.AddAsync(newCase);
            await _dbContext.SaveChangesAsync();
            return newCase;
        }
    }
}
