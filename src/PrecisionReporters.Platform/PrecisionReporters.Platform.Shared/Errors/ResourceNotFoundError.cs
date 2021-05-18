using FluentResults;

namespace PrecisionReporters.Platform.Shared.Errors
{
    public class ResourceNotFoundError : Error
    {
        public ResourceNotFoundError()
        { }

        public ResourceNotFoundError(string message) : base(message)
        { }
    }
}
