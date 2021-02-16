using FluentResults;

namespace PrecisionReporters.Platform.Domain.Errors
{
    public class ForbiddenError : Error
    {
        public ForbiddenError() : base("403")
        { }
    }
}