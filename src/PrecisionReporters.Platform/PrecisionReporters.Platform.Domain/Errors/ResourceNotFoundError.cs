using FluentResults;

namespace PrecisionReporters.Platform.Domain.Errors
{
    public class ResourceNotFoundError : Error
    {
        public ResourceNotFoundError()
        { }

        public ResourceNotFoundError(string message) : base(message)
        { }
    }
}
