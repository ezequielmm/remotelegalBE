using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PrecisionReporters.Platform.Transcript.Api.Authorization.Handlers;
using PrecisionReporters.Platform.Shared.Authorization.Requirements;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Transcript.Api.Authorization
{
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly ILogger<UserAuthorizeHandler> _logger;
        public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options, ILogger<UserAuthorizeHandler> logger)
        {
            // There can only be one policy provider in ASP.NET Core. We only handle permissions
            // related policies, for the rest we will use the default provider.
            FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);

            _logger = logger;
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetFallbackPolicyAsync();

        // Dynamically creates a policy with a requirement that contains the permission. The policy
        // name must match the permission that is needed.
        public async Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            var policy = await FallbackPolicyProvider.GetPolicyAsync(policyName);
            if (policy == null)
            {
                try
                {
                    var policyBuilder = new AuthorizationPolicyBuilder();

                    policyBuilder.AddRequirements(new UserAuthorizeRequirement(policyName));
                    policy = policyBuilder.Build();
                }
                catch (Exception ex)
                {
                    // Log an error. Currently, we don't support any other policies.
                    _logger.LogError($"Failed to create a policy handler for policy={policyName}. Exception: {ex.Message}");
                }
            }
            return policy;
        }
    }
}
