using FluentResults;
using System.Linq;

namespace PrecisionReporters.Platform.Shared.Extensions
{
    public static class ResultExtensions
    {
        public static string GetErrorMessage<T>(this Result<T> result)
        {
            if (result.IsSuccess) return string.Empty;

            return string.Join(", ", result.Errors.Select(e => e.Message));
        }

        public static string GetErrorMessage(this Result result)
        {
            if (result.IsSuccess) return string.Empty;

            return string.Join(", ", result.Errors.Select(e => e.Message));
        }
    }
}
