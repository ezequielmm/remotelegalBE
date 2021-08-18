using System;
using System.Dynamic;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Shared.Helpers.Interfaces
{
    public interface ILoggingHelper
    {
        Task<T> ExecuteWithScope<T>(ExpandoObject scopes, Func<Task<T>> action);
        Task<T> ExecuteWithDeposition<T>(Guid depositionId, Func<Task<T>> action);
    }
}