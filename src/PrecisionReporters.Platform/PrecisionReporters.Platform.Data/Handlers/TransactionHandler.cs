using Microsoft.EntityFrameworkCore;
using PrecisionReporters.Platform.Data.Handlers.Interfaces;
using System;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace PrecisionReporters.Platform.Data.Handlers
{
    public class TransactionHandler : ITransactionHandler
    {
        private readonly IDatabaseTransactionProvider _transactionProvider;
        private readonly ILogger<TransactionHandler> _logger;

        /// <summary>
        /// Creates a TransactionHandler object for a specific database context.
        /// </summary>
        /// <param name="transactionProvider">Database provider to manage transactions</param>
        /// <param name="logger">Logger</param>
        public TransactionHandler(IDatabaseTransactionProvider transactionProvider,
            ILogger<TransactionHandler> logger)
        {
            _transactionProvider = transactionProvider;
            _logger = logger;
        }

        /// <summary>
        /// Performs an action within a transaction and commits it if no exception is thrown.
        /// </summary>
        /// <param name="actionToPerform">action to perform</param>
        public async Task<Result> RunAsync(Func<Task> actionToPerform)
        {
            if (_transactionProvider.CurrentTransaction != null)
            {
                // We are already within a transaction, so just execute normally
                return await RunInternalAsync(actionToPerform);
            }

            var strategy = _transactionProvider.CreateExecutionStrategy();
            return await strategy.Execute(async () =>
            {
                await using var transaction = await _transactionProvider.BeginTransactionAsync();
                var actionResult = await RunInternalAsync(actionToPerform);
                if (actionResult.IsFailed)
                {
                    await transaction.RollbackAsync();
                    return actionResult;
                }

                await transaction.CommitAsync();
                return Result.Ok();
            });
        }

        private async Task<Result> RunInternalAsync(Func<Task> actionToPerform)
        {
            try
            {
                await actionToPerform();
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing transaction operation.");
                return Result.Fail(new ExceptionalError("Error executing transaction operation.", ex));
            }
        }
    }
}
