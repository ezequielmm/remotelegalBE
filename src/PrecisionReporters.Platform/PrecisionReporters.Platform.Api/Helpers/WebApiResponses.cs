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
                return new BadRequestResult();

            if (result.HasError<ResourceNotFoundError>())
                return new NotFoundResult();
            
            if (result.HasError<ResourceConflictError>())
                return new ConflictResult();

            return new StatusCodeResult((int) HttpStatusCode.InternalServerError);
        }
    }
}
