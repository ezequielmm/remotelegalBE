using FluentResults;

namespace PrecisionReporters.Platform.Domain.Errors
{
    public class UnexpectedError : Error
    {
        public UnexpectedError()
        { }

        public UnexpectedError(string message) : base(message)
        { }
    }
}
