using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PrecisionReporters.Platform.Shared.Helpers
{
    public class ForbiddenErrorResult : ObjectResult
    {
        public ForbiddenErrorResult(object value) : base(value)
        {
            this.StatusCode = StatusCodes.Status403Forbidden;
        }
    }
}