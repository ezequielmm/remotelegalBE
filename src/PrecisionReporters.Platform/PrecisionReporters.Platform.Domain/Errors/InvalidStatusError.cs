using FluentResults;

namespace PrecisionReporters.Platform.Domain.Errors
{
    class InvalidStatusError : Error
    {
        public InvalidStatusError()
        { }

        public InvalidStatusError(string message) : base(message)
        { }
    }
}