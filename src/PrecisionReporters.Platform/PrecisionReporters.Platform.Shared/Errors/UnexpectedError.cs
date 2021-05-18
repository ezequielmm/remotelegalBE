using FluentResults;

namespace PrecisionReporters.Platform.Shared.Errors
{
    public class UnexpectedError : Error
    {
        public UnexpectedError()
        { }

        public UnexpectedError(string message) : base(message)
        { }
    }
}
