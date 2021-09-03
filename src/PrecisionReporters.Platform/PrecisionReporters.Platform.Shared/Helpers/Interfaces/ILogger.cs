using System;

namespace PrecisionReporters.Platform.Shared.Helpers.Interfaces
{
    public interface ILogger
    {
        void LogInformation(string messageTemplate, params object[] arguments);
        void LogError(string messageTemplate, params object[] arguments);
        void LogError(Exception exception, string messageTemplate, params object[] arguments);
        void LogWarning(string messageTemplate, params object[] arguments);
    }
}