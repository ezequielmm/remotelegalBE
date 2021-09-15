using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services.Interfaces
{
    public interface IFireAndForgetService
    {
        void Execute<TRequiredService>(Func<TRequiredService, Task> action);
    }
}
