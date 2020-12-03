using Microsoft.AspNetCore.Authorization;
using PrecisionReporters.Platform.Data.Enums;
using System;

namespace PrecisionReporters.Platform.Api.Authorization.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class UserAuthorizeAttribute : AuthorizeAttribute
    {
        // TODO: In this first approach we get a single resource and an action. In the future we will probably need the whole resource tree
        public UserAuthorizeAttribute(ResourceType resourceType, ResourceAction action)
            : base($"{resourceType}.{action}")
        {
        }
    }
}
