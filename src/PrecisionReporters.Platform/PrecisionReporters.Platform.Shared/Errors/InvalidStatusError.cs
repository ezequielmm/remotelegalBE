using FluentResults;

namespace PrecisionReporters.Platform.Shared.Errors
{
    public class InvalidStatusError : Error
    {
        public InvalidStatusError()
        { }

        public InvalidStatusError(string message) : base(message)
        { }
    }
}