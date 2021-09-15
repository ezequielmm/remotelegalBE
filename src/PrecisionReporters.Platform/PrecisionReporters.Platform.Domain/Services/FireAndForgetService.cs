using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class FireAndForgetService : IFireAndForgetService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger _logger;

        public FireAndForgetService(IServiceScopeFactory serviceScopeFactory, ILogger<FireAndForgetService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public void Execute<TRequiredService>(Func<TRequiredService, Task> action)
        {
            Task.Run(async () =>
            {
                _logger.LogDebug("Registered fire and forget task for service {Service}.", typeof(TRequiredService).Name);
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var injectedService = scope.ServiceProvider.GetRequiredService<TRequiredService>();
                    await action(injectedService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An exception occurred while trying to execute the fire and forget task for service {Service}.", typeof(TRequiredService).Name);
                }
            });
        }
    }
}
