using Microsoft.EntityFrameworkCore;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Data.Handlers
{
    public class TransactionHandler : ITransactionHandler
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ITransactionInfo _transactionInfo;

        /// <summary>
        /// Creates a TransactionHandler object for a specific database context.
        /// </summary>
        /// <param name="dbContext">Database context to manage transactions</param>
        /// <param name="transactionInfo"></param>
        public TransactionHandler(ApplicationDbContext dbContext, ITransactionInfo transactionInfo)
        {
            _dbContext = dbContext;
            _transactionInfo = transactionInfo;
        }

        /// <summary>
        /// Performs an action within a transaction and commits it if no exception is thrown.
        /// </summary>
        /// <param name="actionToPerform">action to perform</param>
        /// <param name="exceptionHandler">Action that will be performed if there is an exception</param>
        public async Task RunAsync(Func<Task> actionToPerform, Func<Exception, Task> exceptionHandler = null)
        {
            if (_dbContext.Database.CurrentTransaction == null)
            {
                var strategy = _dbContext.Database.CreateExecutionStrategy();
                await strategy.Execute(async () =>
                {
                    using (var transaction = await _dbContext.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            await actionToPerform();
                            // Update the record of what happened in this transaction just in case
                            // SaveChangesAsync was never called
                            var transactionId = transaction.TransactionId;
                            _transactionInfo.UpdateTransactionInfo(transactionId, _dbContext.ChangeTracker);
                            await transaction.CommitAsync();
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            if (exceptionHandler != null)
                            {
                                await exceptionHandler(ex);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                });
            }
            else
            {
                //We are already within a transaction, so just execute normally
                await actionToPerform();
            }
        }
    }
}
