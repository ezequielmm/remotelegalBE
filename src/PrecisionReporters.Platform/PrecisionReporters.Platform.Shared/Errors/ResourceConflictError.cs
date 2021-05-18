using FluentResults;

namespace PrecisionReporters.Platform.Shared.Errors
{
    public class ResourceConflictError : Error
    {
        public ResourceConflictError()
        { }
        
        public ResourceConflictError(string message) : base(message)
        { }
    }
}