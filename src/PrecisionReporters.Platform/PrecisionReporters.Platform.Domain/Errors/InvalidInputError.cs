using FluentResults;

namespace PrecisionReporters.Platform.Domain.Errors
{
    public class InvalidInputError : Error
    {
        public InvalidInputError()
        { }

        public InvalidInputError(string message) : base(message)
        { }
    }
}
