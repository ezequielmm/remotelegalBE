using Microsoft.AspNetCore.Http;
using System;

namespace PrecisionReporters.Platform.Shared.Extensions
{
    public static class HttpContextExtensions
    {
        public static Guid? GetRouteGuid(this HttpContext context, string parameterName)
        {
            if (context.Request.RouteValues.TryGetValue(parameterName, out var parameterValue) && Guid.TryParse((string)parameterValue, out var parsedValue))
            {
                return parsedValue;
            }
            else
            {
                return null;
            }
        }
    }
}
