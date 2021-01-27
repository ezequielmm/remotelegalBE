using System;
using System.Threading.Tasks;
using FluentResults;

namespace PrecisionReporters.Platform.Data.Handlers.Interfaces
{
    public interface ITransactionHandler
    {
        /// <summary>
        /// Performs an action within a transaction and commits it if no exception is thrown.
        /// </summary>
        /// <param name="actionToPerform">action to perform</param>
        Task<Result> RunAsync(Func<Task> actionToPerform);

        /// <summary>
        /// Performs an action within a transaction and commits it if result is OK.
        /// </summary>
        /// <param name="actionToPerform">action to perform</param>
        Task<Result<T>> RunAsync<T>(Func<Task<Result<T>>> actionToPerform);
    }
}