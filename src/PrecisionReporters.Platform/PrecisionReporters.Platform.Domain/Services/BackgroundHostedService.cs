using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class BackgroundHostedService : BackgroundService
    {
        private readonly ILogger<BackgroundHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public BackgroundHostedService(IBackgroundTaskQueue taskQueue,
            ILogger<BackgroundHostedService> logger, IServiceProvider serviceProvider)
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
                var dequeResult = await TaskQueue.DequeueAsync(stoppingToken);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var processType = string.Empty;
                    try
                    {
                        switch (dequeResult.TaskType)
                        {
                            case BackgroundTaskType.DraftTranscription:
                                processType = Enum.GetName(typeof(BackgroundTaskType), BackgroundTaskType.DraftTranscription);
                                var draftTranscriptDto = dequeResult.Content as DraftTranscriptDto;
                                var scopedDraftTranscription = scope.ServiceProvider.GetRequiredService<IDraftTranscriptGeneratorService>();
                                await scopedDraftTranscription.GenerateDraftTranscription(draftTranscriptDto);
                                break;
                            case BackgroundTaskType.DeleteTwilioComposition:
                                processType = Enum.GetName(typeof(BackgroundTaskType), BackgroundTaskType.DeleteTwilioComposition);
                                var deleteTwilioRecordings = dequeResult.Content as DeleteTwilioRecordingsDto;
                                var scopedDeleteRecordings = scope.ServiceProvider.GetRequiredService<ICompositionService>();
                                await scopedDeleteRecordings.DeleteTwilioCompositionAndRecordings(deleteTwilioRecordings);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            $"Error occurred executing {processType} BackGround Process.");
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
