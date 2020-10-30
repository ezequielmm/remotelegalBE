using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Data.Handlers.Interfaces
{
    public interface ITransactionHandler
    {
        Task RunAsync(Func<Task> actionToPerform, Func<Exception, Task> exceptionHandler = null);
    }
}
