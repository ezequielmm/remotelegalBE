using Microsoft.AspNetCore.Authorization;
using PrecisionReporters.Platform.Data.Enums;
using System;

namespace PrecisionReporters.Platform.Api.Authorization.Requirements
{
    public class UserAuthorizeRequirement: IAuthorizationRequirement
    {
        public ResourceType ResourceType { get; set; }
        public ResourceAction ResourceAction { get; set; }

        public UserAuthorizeRequirement(string permissions)
        {
            // TODO: In this first approach we get a single resource and an action. In the future we will probably need the whole resource tree
            var parsedPermissions = permissions.Split('.');
            ResourceType = Enum.Parse<ResourceType>(parsedPermissions[0]);
            ResourceAction = Enum.Parse<ResourceAction>(parsedPermissions[1]);
        }
    }
}
