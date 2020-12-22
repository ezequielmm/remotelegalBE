using System.Net;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Domain.Errors;

namespace PrecisionReporters.Platform.Api.Helpers
{
    public static class WebApiResponses
    {
        public static ActionResult GetErrorResponse(Result result)
        {
            if (result.HasError<InvalidInputError>())
                return new BadRequestObjectResult (result.Errors);

            if (result.HasError<ResourceNotFoundError>())
                return new NotFoundObjectResult(result.Errors);
            
            if (result.HasError<ResourceConflictError>())
                return new ConflictObjectResult(result.Errors);

            return new StatusCodeResult((int) HttpStatusCode.InternalServerError);
        }
    }
}
