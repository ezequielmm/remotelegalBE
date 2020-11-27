using FluentResults;

namespace PrecisionReporters.Platform.Domain.Errors
{
    public class ResourceConflictError : Error
    {
        public ResourceConflictError()
        { }
        
        public ResourceConflictError(string message) : base(message)
        { }
    }
}