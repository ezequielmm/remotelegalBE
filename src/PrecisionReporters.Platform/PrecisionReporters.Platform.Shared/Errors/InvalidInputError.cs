using FluentResults;

namespace PrecisionReporters.Platform.Shared.Errors
{
    public class InvalidInputError : Error
    {
        public InvalidInputError()
        { }

        public InvalidInputError(string message) : base(message)
        { }
    }
}
