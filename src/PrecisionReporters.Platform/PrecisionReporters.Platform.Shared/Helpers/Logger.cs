using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using System;

namespace PrecisionReporters.Platform.Shared.Helpers
{
    public class Logger: Interfaces.ILogger
    {
        private static Serilog.Core.Logger _logger;

        public Logger()
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext().Enrich.WithEnvironmentUserName().Enrich.WithExceptionDetails()
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .CreateLogger();
        }

        public void LogInformation(string messageTemplate, params object[] arguments)
        {
            _logger.Information(messageTemplate, arguments);
        }

        public void LogError(string messageTemplate, params object[] arguments)
        {
            _logger.Error(messageTemplate, arguments);
        }

        public void LogError(Exception exception, string messageTemplate, params object[] arguments)
        {
            _logger.Error(exception, messageTemplate, arguments);
        }

        public void LogWarning(string messageTemplate, params object[] arguments)
        {
            _logger.Warning(messageTemplate, arguments);
        }
    }
}