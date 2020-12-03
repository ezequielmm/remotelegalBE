using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Api.Authorization.Requirements;
using PrecisionReporters.Platform.Api.Extensions;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Api.Authorization.Handlers
{
    public class UserAuthorizeHandler : AuthorizationHandler<UserAuthorizeRequirement, Endpoint>
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<UserAuthorizeHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserAuthorizeHandler(IPermissionService permissionService, ILogger<UserAuthorizeHandler> logger, IHttpContextAccessor httpContextAccessor)
        {
            _permissionService = permissionService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, UserAuthorizeRequirement requirement, Endpoint resource)
        {
            if (context.TryGetRouteIdNameOfResourceType(requirement.ResourceType, out var resourceId) && _httpContextAccessor.HttpContext.GetRouteGuid(resourceId) is Guid identifier)
            {
                var currentUserEmail = context.User.FindFirstValue(ClaimTypes.Email);
                if (await _permissionService.CheckUserHasPermissionForAction(currentUserEmail, requirement.ResourceType, identifier, requirement.ResourceAction))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogError($"User does not have '{requirement.ResourceAction}' permissions on '{requirement.ResourceType}.{resource}'");
                    context.Fail();
                }
            }
            else
            {
                _logger.LogError($"Failed to extract the identity of the resource '{resource}'");
                context.Fail();
            }
        }
    }
}
