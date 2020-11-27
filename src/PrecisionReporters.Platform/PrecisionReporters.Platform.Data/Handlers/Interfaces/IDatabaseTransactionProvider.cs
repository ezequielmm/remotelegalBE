using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace PrecisionReporters.Platform.Data.Handlers.Interfaces
{
    public interface IDatabaseTransactionProvider
    {
        IDbContextTransaction CurrentTransaction { get; }
        IExecutionStrategy CreateExecutionStrategy();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}