using PrecisionReporters.Platform.Shared.Helpers.Interfaces;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Shared.Helpers
{
    public class LoggingHelper : ILoggingHelper
    {
        public async Task<T> ExecuteWithScope<T>(ExpandoObject scopes, Func<Task<T>> action)
        {
            IDictionary<string, object> scopesDictionary = scopes;
            var props = scopesDictionary.Select(x => LogContext.PushProperty(x.Key, x.Value)).ToList();

            var result = await action.Invoke();

            props.ForEach(x => x.Dispose());

            return result;
        }

        public async Task<T> ExecuteWithDeposition<T>(Guid depositionId, Func<Task<T>> action)
        {
            dynamic scopes = new ExpandoObject();
            scopes.DepositionId = depositionId;
            return await ExecuteWithScope<T>(scopes, action);
        }
    }
}
