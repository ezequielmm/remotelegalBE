using FluentResults;
using System.Net;

namespace PrecisionReporters.Platform.Domain.Errors
{
    public class ForbiddenError : Error
    {
        public ForbiddenError() : base("403")
        { }
    }
}