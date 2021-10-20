using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using PrecisionReporters.Platform.Shared.Attributes;
using PrecisionReporters.Platform.Shared.Authorization.Attributes;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Shared.Extensions;
using Serilog.Context;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Shared.Middlewares
{
    public class LogResourceMiddleware
    {
        private readonly RequestDelegate _next;

        public LogResourceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
            var attribute = endpoint?.Metadata.GetMetadata<UserAuthorizeAttribute>();
            if (attribute != null)
            {
                attribute.ResourceType = attribute.Policy.Split('.')[0];
                var resource = (ResourceType)Enum.Parse(typeof(ResourceType), attribute.ResourceType);
                var paramName = GetParamName(endpoint, resource);
                var id = context.GetRouteGuid(paramName);
                var logObject = GetLogObject(resource,id.ToString());
                using (LogContext.PushProperty("scope", logObject, true)) { return _next(context); };
            }
            return _next(context);
        }

        private string GetParamName(Endpoint endpoint, ResourceType resource)
        {
            var controllerDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            var name = controllerDescriptor.MethodInfo.GetParameters().FirstOrDefault(parameter => parameter.GetCustomAttribute(typeof(ResourceIdAttribute), false) is ResourceIdAttribute x && x.ResourceType == resource)?.Name;
            return name;
        }

        private object GetLogObject(ResourceType resource, string value)
        {
            //TODO: Find a better approach for doing this 
            switch (resource)
            {
                case ResourceType.Deposition:
                    return new { DepositionId = value };
                case ResourceType.Case:
                    return new { CaseId = value };
                case ResourceType.Document:
                    return new { DocumentId = value };
                case ResourceType.User:
                    return new { UserId = value };
                default:
                    return new { Id = value };
            }
        }
    }
}
