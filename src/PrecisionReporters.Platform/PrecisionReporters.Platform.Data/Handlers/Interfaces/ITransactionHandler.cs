using System;
using System.Threading.Tasks;
using FluentResults;

namespace PrecisionReporters.Platform.Data.Handlers.Interfaces
{
    public interface ITransactionHandler
    {
        Task<Result> RunAsync(Func<Task> actionToPerform);
    }
}