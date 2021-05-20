using FluentResults;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Enums;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class CompositionService : ICompositionService
    {
        private readonly ICompositionRepository _compositionRepository;
        private readonly ITwilioService _twilioService;
        private readonly IRoomService _roomService;
        private readonly IDepositionService _depositionService;
        private readonly ILogger<CompositionService> _logger;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;

        public CompositionService(ICompositionRepository compositionRepository,
            ITwilioService twilioService, IRoomService roomService, IDepositionService depositionService,
            ILogger<CompositionService> logger, IBackgroundTaskQueue backgroundTaskQueue
            )
        {
            _compositionRepository = compositionRepository;
            _twilioService = twilioService;
            _roomService = roomService;
            _depositionService = depositionService;
            _logger = logger;
            _backgroundTaskQueue = backgroundTaskQueue;
        }

        public async Task<Result> StoreCompositionMediaAsync(Composition composition)
        {
            composition.EndDate = DateTime.UtcNow;
            var couldDownloadComposition = await _twilioService.GetCompositionMediaAsync(composition);
            if (!couldDownloadComposition)
                return Result.Fail(new ExceptionalError("Could now download composition media.", null));

            composition.Status = CompositionStatus.Uploading;
            var updatedComposition = await UpdateComposition(composition);

            await _twilioService.UploadCompositionMediaAsync(updatedComposition);
            return Result.Ok();
        }

        public async Task<Result<Composition>> GetCompositionByRoom(Guid roomSid)
        {
            var composition = await _compositionRepository.GetFirstOrDefaultByFilter(x => x.RoomId == roomSid);
            if (composition == null)
                return Result.Fail(new ResourceNotFoundError());

            return Result.Ok(composition);
        }

        public async Task<Composition> UpdateComposition(Composition composition)
        {
            composition.LastUpdated = DateTime.UtcNow;
            return await _compositionRepository.Update(composition);
        }

        public async Task<Result<Composition>> UpdateCompositionCallback(Composition composition)
        {
            var roomResult = await _roomService.GetRoomBySId(composition.Room.SId);
            if (roomResult.IsFailed)
                return roomResult.ToResult<Composition>();

            var compositionToUpdate = await _compositionRepository.GetFirstOrDefaultByFilter(x => x.SId == composition.SId);
            if (compositionToUpdate == null)
                return Result.Fail(new ResourceNotFoundError());

            var depositionResult = await _depositionService.GetDepositionByRoomId(roomResult.Value.Id);
            if (depositionResult.IsFailed)
                return depositionResult.ToResult<Composition>();

            compositionToUpdate.Status = depositionResult.Value.Events.Any(x => x.EventType == EventType.OnTheRecord) ? composition.Status : CompositionStatus.Empty;
                
            if (compositionToUpdate.Status == CompositionStatus.Available)
            {
                compositionToUpdate.MediaUri = composition.MediaUri;

                var uploadMetadataResult = await UploadCompositionMetadata(depositionResult.Value);
                if (uploadMetadataResult.IsFailed)
                    return uploadMetadataResult.ToResult<Composition>();

                var storeCompositionResult = await StoreCompositionMediaAsync(compositionToUpdate);
                compositionToUpdate.Status = storeCompositionResult.IsSuccess
                    ? CompositionStatus.Stored
                    : CompositionStatus.UploadFailed;
            }

            var updatedComposition = await UpdateComposition(compositionToUpdate);

            if (updatedComposition.Status == CompositionStatus.Empty)
            {
                var deleteTwilioRecordings = new DeleteTwilioRecordingsDto() { RoomSid = updatedComposition.Room.SId.Trim(), CompositionSid = updatedComposition.SId.Trim() };
                var backGround = new BackgroundTaskDto() { Content = deleteTwilioRecordings, TaskType = BackgroundTaskType.DeleteTwilioComposition };
                _backgroundTaskQueue.QueueBackgroundWorkItem(backGround);
            }

            return Result.Ok(updatedComposition);
        }

        private CompositionRecordingMetadata CreateCompositioMetadata(Deposition deposition)
        {
            var startDateTime = _twilioService.GetVideoStartTimeStamp(deposition.Room.SId);
            return new CompositionRecordingMetadata
            {
                //TODO unified file name generation in one place
                Video = $"{deposition.Room.Composition.SId}.{ApplicationConstants.Mp4}",
                Name = deposition.Room.Composition.SId,
                TimeZone = Enum.GetValues(typeof(USTimeZone)).Cast<USTimeZone>().FirstOrDefault(x => x.GetDescription() == deposition.TimeZone).ToString(),
                TimeZoneDescription = deposition.TimeZone,
                OutputFormat = deposition.Room.Composition.FileType,
                StartDate = startDateTime.Result.Value,
                EndDate = GetDateTimestamp(deposition.Room.EndDate.Value),
                Intervals = GetDepositionRecordingIntervals(deposition.Events, startDateTime.Result.Value)
            };
        }

        private async Task<Result> UploadCompositionMetadata(Deposition deposition)
        {
            var metadata = CreateCompositioMetadata(deposition);
            return await _twilioService.UploadCompositionMetadata(metadata);

        }

        public List<CompositionInterval> GetDepositionRecordingIntervals(List<DepositionEvent> events, long startTime)
        {
            var result = events
                .OrderBy(x => x.CreationDate)
                .Where(x => x.EventType == EventType.OnTheRecord || x.EventType == EventType.OffTheRecord)
                .Aggregate(new List<CompositionInterval>(),
                (list, x) =>
                {
                    if (x.EventType == EventType.OnTheRecord)
                    {
                        var compositionInterval = new CompositionInterval
                        {
                            Start = CalculateSeconds(startTime, GetDateTimestamp(x.CreationDate))
                        };
                        list.Add(compositionInterval);
                    }
                    if (x.EventType == EventType.OffTheRecord)
                        list.Last().Stop = CalculateSeconds(startTime, GetDateTimestamp(x.CreationDate));

                    return list;
                });

            return result;
        }

        private long GetDateTimestamp(DateTime date)
        {
            return new DateTimeOffset(date, TimeSpan.Zero).ToUnixTimeSeconds();
        }

        private int CalculateSeconds(long startTime, long splitTime)
        {
            return (int)(splitTime - startTime);
        }

        public async Task<Result> PostDepoCompositionCallback(PostDepositionEditionDto message)
        {
            var includes = new[] { nameof(Composition.Room) };
            var composition = await _compositionRepository.GetFirstOrDefaultByFilter(x => x.SId == message.GetCompositionId(), includes);
            if (composition == null)
                return Result.Fail(new ResourceNotFoundError());

            composition.Status = message.IsComplete()
                ? CompositionStatus.Completed
                : CompositionStatus.EditionFailed;

            composition.LastUpdated = DateTime.UtcNow;

            await _compositionRepository.Update(composition);
            var deleteTwilioRecordings = new DeleteTwilioRecordingsDto() { RoomSid = composition.Room.SId.Trim(), CompositionSid = composition.SId.Trim() };
            var backGround = new BackgroundTaskDto() { Content = deleteTwilioRecordings, TaskType = BackgroundTaskType.DeleteTwilioComposition };
            _backgroundTaskQueue.QueueBackgroundWorkItem(backGround);
            return Result.Ok();
        }

        // This method is meant to validate and confirm the endpoind added
        // as a valid destination to receive messages from aws notification service
        public async Task<Result> SubscribeEndpoint(string subscribeURL)
        {
            var request = (HttpWebRequest)WebRequest.Create(subscribeURL);
            try
            {
                await request.GetResponseAsync();
            }
            catch (Exception e)
            {
                _logger.LogError($"There was an error subscribing URL, {e.Message}");
                return Result.Fail(new Error("There was an error subscribing URL"));
            }

            return Result.Ok();
        }

        public async Task<Result> DeleteTwilioCompositionAndRecordings(DeleteTwilioRecordingsDto deleteTwilioRecordings)
        {
            await _twilioService.DeleteCompositionAndRecordings(deleteTwilioRecordings);
            return Result.Ok();
        }
    }
}
