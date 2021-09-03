using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using PrecisionReporters.Platform.Shared.Attributes;
using PrecisionReporters.Platform.Shared.Authorization.Attributes;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Shared.Extensions;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Shared.Middlewares
{
    public class LogResourceMiddleware
    {
        private readonly RequestDelegate _next;
        private const string Parameter_Name = "id";

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
                attribute.ResourceId = id.Value;
                return SetLog(attribute, context);
            }
            return _next(context);
        }

        private Task SetLog(UserAuthorizeAttribute attribute, HttpContext context)
        {
            dynamic scopeObject = new ExpandoObject();
            IDictionary<string, object> scope = (IDictionary<string, object>)scopeObject;
            //Create dynamic object depending on key value dictionary
            scope.Add($"{attribute.ResourceType}Id", $"{attribute.ResourceId}");
            using (LogContext.PushProperty("scope", scopeObject, true)) { return _next(context); };
        }

        private string GetParamName(Endpoint endpoint, ResourceType resource)
        {
            var controllerDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            var name = controllerDescriptor.MethodInfo.GetParameters().FirstOrDefault(parameter => parameter.GetCustomAttribute(typeof(ResourceIdAttribute), false) is ResourceIdAttribute x && x.ResourceType == resource)?.Name;
            return name;
        }
    }
}
