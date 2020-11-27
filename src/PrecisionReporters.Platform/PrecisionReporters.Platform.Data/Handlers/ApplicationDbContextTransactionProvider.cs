using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;

namespace PrecisionReporters.Platform.Data.Handlers
{
    public class ApplicationDbContextTransactionProvider: IDatabaseTransactionProvider
    {
        private readonly ApplicationDbContext _dbContext;

        public ApplicationDbContextTransactionProvider(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IDbContextTransaction CurrentTransaction => _dbContext.Database.CurrentTransaction;

        public IExecutionStrategy CreateExecutionStrategy() => _dbContext.Database.CreateExecutionStrategy();

        public async Task<IDbContextTransaction> BeginTransactionAsync() => await _dbContext.Database.BeginTransactionAsync();
    }
}