using Microsoft.AspNetCore.Authorization;
using PrecisionReporters.Platform.Shared.Enums;
using System;

namespace PrecisionReporters.Platform.Shared.Authorization.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class UserAuthorizeAttribute : AuthorizeAttribute
    {
        public Guid ResourceId { get; set; }
        public string ResourceType { get; set; }
        // TODO: In this first approach we get a single resource and an action. In the future we will probably need the whole resource tree
        public UserAuthorizeAttribute(ResourceType resourceType, ResourceAction action)
            : base($"{resourceType}.{action}")
        {
        }
    }
}
