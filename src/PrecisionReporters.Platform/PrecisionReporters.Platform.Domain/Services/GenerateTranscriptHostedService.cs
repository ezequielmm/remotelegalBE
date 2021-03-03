using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrecisionReporters.Platform.Domain.Services.Interfaces;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class GenerateTranscriptHostedService : BackgroundService
    {
        private readonly ILogger<GenerateTranscriptHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public GenerateTranscriptHostedService(IBackgroundTaskQueue taskQueue,
            ILogger<GenerateTranscriptHostedService> logger, IServiceProvider serviceProvider)
        {
            TaskQueue = taskQueue;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public IBackgroundTaskQueue TaskQueue { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                $"Queued Hosted Service is running.{Environment.NewLine}" +
                $"{Environment.NewLine}Tap W to add a work item to the " +
                $"background queue.{Environment.NewLine}");

            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var draftTranscriptDto = await TaskQueue.DequeueAsync(stoppingToken);

                using (var scope = _serviceProvider.CreateScope())
                {
                    try
                    {
                        var scopedProcessingService = scope.ServiceProvider.GetRequiredService<IDraftTranscriptGeneratorService>();
                        await scopedProcessingService.GenerateDraftTranscriptionPDF(draftTranscriptDto);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error occurred executing Draft Transcript BackGround Process.", nameof(draftTranscriptDto.DepositionId));
                    }
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued Hosted Service is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}
