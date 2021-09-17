using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PrecisionReporters.Platform.Shared.Helpers
{
    public class InternalServerErrorResult: ObjectResult
    {
        public InternalServerErrorResult(object value): base(value)
        {
            this.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
}
