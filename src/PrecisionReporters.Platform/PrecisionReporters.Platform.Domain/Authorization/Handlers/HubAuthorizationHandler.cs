using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Domain.Authorization.Requirements;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Authorization.Handlers
{
    public class HubAuthorizeHandler : AuthorizationHandler<UserAuthorizeRequirement, HubInvocationContext>
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<UserAuthorizeHandler> _logger;

        public HubAuthorizeHandler(IPermissionService permissionService, ILogger<UserAuthorizeHandler> logger)
        {
            _permissionService = permissionService;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, UserAuthorizeRequirement requirement, HubInvocationContext resource)
        {
            var identifier = resource.GetResourceIdFromHubMethod(requirement.ResourceType);
            if (identifier.HasValue)
            {
                var currentUserEmail = context.User.FindFirstValue(ClaimTypes.Email);
                if (await _permissionService.CheckUserHasPermissionForAction(currentUserEmail, requirement.ResourceType, identifier.Value, requirement.ResourceAction))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogError("User does not have {0} permissions on {1}.{2} for user's email: {3}", requirement.ResourceAction, requirement.ResourceType, resource, currentUserEmail);
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
