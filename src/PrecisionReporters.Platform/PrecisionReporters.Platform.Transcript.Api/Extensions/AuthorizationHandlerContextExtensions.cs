using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Attributes;
using System.Linq;
using System.Reflection;

namespace PrecisionReporters.Platform.Transcript.Api.Extensions
{
    public static class AuthorizationHandlerContextExtensions
    {
        public static bool TryGetRouteIdNameOfResourceType(this AuthorizationHandlerContext context, ResourceType resource, out string name)
        {
            if (context.Resource is Endpoint endpoint && endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is ControllerActionDescriptor controllerDescriptor)
            {
                name = controllerDescriptor.MethodInfo.GetParameters().FirstOrDefault(parameter => parameter.GetCustomAttribute(typeof(ResourceIdAttribute), false) is ResourceIdAttribute x && x.ResourceType == resource)?.Name;
                return !string.IsNullOrWhiteSpace(name);
            }
            else
            {
                name = null;
                return false;
            }
        }
    }
}
