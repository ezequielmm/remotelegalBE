using FluentResults;

namespace PrecisionReporters.Platform.Shared.Errors
{
    public class ForbiddenError : Error
    {
        public ForbiddenError() : base("403")
        { }
    }
}