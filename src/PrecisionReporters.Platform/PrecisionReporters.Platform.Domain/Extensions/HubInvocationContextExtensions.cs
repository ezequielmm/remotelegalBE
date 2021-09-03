using Microsoft.AspNetCore.SignalR;
using PrecisionReporters.Platform.Shared.Enums;
using PrecisionReporters.Platform.Shared.Attributes;
using System;
using System.Linq;

namespace PrecisionReporters.Platform.Domain.Extensions
{
    public static class HubInvocationContextExtensions
    {
        public static Guid? GetResourceIdFromHubMethod(this HubInvocationContext context, ResourceType resource)
        {
            var resourceIdArgs = from arg in context.HubMethodArguments
                                 let argProperties = arg.GetType().GetProperties()
                                 from prop in argProperties
                                 where prop.GetCustomAttributes(typeof(ResourceIdAttribute), false).Cast<ResourceIdAttribute>().FirstOrDefault()?.ResourceType == resource
                                 select (Argument: arg, IdentifierProperty: prop);

            var (argument, propertyInfo) = resourceIdArgs.FirstOrDefault();
            return (Guid?)propertyInfo?.GetValue(argument);
        }
    }
}
