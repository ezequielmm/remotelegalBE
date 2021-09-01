using PrecisionReporters.Platform.Shared.Helpers.Interfaces;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Shared.Enums;

namespace PrecisionReporters.Platform.Shared.Helpers
{
    public class LoggingHelper : ILoggingHelper
    {
        private readonly ILogger<LoggingHelper> _logger;

        public LoggingHelper(ILogger<LoggingHelper> logger)
        {
            _logger = logger;
        }
        
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

        public async Task LogInformationWithScope(LogCategory category, string message)
        {
            dynamic scopes = new ExpandoObject();
            scopes.Category = category;
            Func<Task> action = async () =>
            {
                await Task.Run(() => { _logger.LogInformation(message); });
            };
            await ExecuteWithScope(scopes, action);
        }
    }
}
