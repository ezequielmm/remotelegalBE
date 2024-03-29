﻿using FluentResults;
using Microsoft.AspNetCore.Mvc;
using PrecisionReporters.Platform.Shared.Errors;

namespace PrecisionReporters.Platform.Shared.Helpers
{
    public static class WebApiResponses
    {
        public static ActionResult GetErrorResponse(Result result)
        {
            if (result.HasError<InvalidInputError>() || result.HasError<InvalidStatusError>())
                return new BadRequestObjectResult(result.Errors);

            if (result.HasError<ResourceNotFoundError>())
                return new NotFoundObjectResult(result.Errors);

            if (result.HasError<ResourceConflictError>())
                return new ConflictObjectResult(result.Errors);

            if (result.HasError<ForbiddenError>())
                return new ForbiddenErrorResult(result.Errors);

            return new InternalServerErrorResult(result.Errors);
        }
    }
}
