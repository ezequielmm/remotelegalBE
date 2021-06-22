using FluentResults;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Enums;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Helpers.Interfaces;
using PrecisionReporters.Platform.Domain.QueuedBackgroundTasks.Interfaces;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Errors;
using System;
using System.Linq;
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
        private readonly ICompositionHelper _compositionHelper;

        public CompositionService(ICompositionRepository compositionRepository,
            ITwilioService twilioService, IRoomService roomService, IDepositionService depositionService,
            ILogger<CompositionService> logger, IBackgroundTaskQueue backgroundTaskQueue, ICompositionHelper compositionHelper
            )
        {
            _compositionRepository = compositionRepository;
            _twilioService = twilioService;
            _roomService = roomService;
            _depositionService = depositionService;
            _logger = logger;
            _backgroundTaskQueue = backgroundTaskQueue;
            _compositionHelper = compositionHelper;
        }

        public async Task<Result> StoreCompositionMediaAsync(Composition composition)
        {
            composition.EndDate = DateTime.UtcNow;
            var couldDownloadComposition = await _twilioService.GetCompositionMediaAsync(composition);
            if (!couldDownloadComposition)
            {
                _logger.LogError("Could not download composition media.");
                return Result.Fail(new ExceptionalError("Could not download composition media.", null));
            }

            composition.Status = CompositionStatus.Uploading;
            var updatedComposition = await UpdateComposition(composition);

            await _twilioService.UploadCompositionMediaAsync(updatedComposition);
            return Result.Ok();
        }

        public async Task<Result<Composition>> GetCompositionByRoom(Guid roomSid)
        {
            var composition = await _compositionRepository.GetFirstOrDefaultByFilter(x => x.RoomId == roomSid);
            if (composition == null)
            {
                _logger.LogError("There was an error trying to get composition of room SId: {0}", roomSid);
                return Result.Fail(new ResourceNotFoundError());
            }

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
            if (roomResult.Value == null)
            {
                _logger.LogError("Room with Sid: {0} not found", composition.Room.SId);
                return roomResult.ToResult<Composition>();
            }

            var compositionToUpdate = await _compositionRepository.GetFirstOrDefaultByFilter(x => x.SId == composition.SId);
            if (compositionToUpdate == null)
            {
                _logger.LogError("Composition with Sid: {0} not found", composition.SId);
                return Result.Fail(new ResourceNotFoundError());
            }

            var depositionResult = await _depositionService.GetDepositionByRoomId(roomResult.Value.Id);
            if (depositionResult.IsFailed)
            {
                _logger.LogError("Deposition with RoomId = {0} not found", roomResult.Value.Id);
                return depositionResult.ToResult<Composition>();
            }

            compositionToUpdate.Status = depositionResult.Value.Events.Any(x => x.EventType == EventType.OnTheRecord) ? composition.Status : CompositionStatus.Empty;
                
            if (compositionToUpdate.Status == CompositionStatus.Available)
            {
                compositionToUpdate.MediaUri = composition.MediaUri;

                var uploadMetadataResult = await UploadCompositionMetadata(depositionResult.Value);
                if (uploadMetadataResult.IsFailed)
                {
                    _logger.LogError("Error uploading composition metadata file from composition SId: {0}", composition.SId);
                    return uploadMetadataResult.ToResult<Composition>();
                }

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

        private CompositionRecordingMetadata CreateCompositionMetadata(Deposition deposition)
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
                EndDate = _compositionHelper.GetDateTimestamp(deposition.Room.EndDate.Value),
                Intervals = _compositionHelper.GetDepositionRecordingIntervals(deposition.Events, startDateTime.Result.Value)
            };
        }

        private async Task<Result> UploadCompositionMetadata(Deposition deposition)
        {
            var metadata = CreateCompositionMetadata(deposition);
            return await _twilioService.UploadCompositionMetadata(metadata);

        }
        
        public async Task<Result> PostDepoCompositionCallback(PostDepositionEditionDto message)
        {
            var includes = new[] { nameof(Composition.Room) };
            var composition = await _compositionRepository.GetFirstOrDefaultByFilter(x => x.SId == message.GetCompositionId(), includes);
            if (composition == null)
            {
                _logger.LogError("Composition not found from payload: {0}", message);
                return Result.Fail(new ResourceNotFoundError());
            }

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

        public async Task<Result> DeleteTwilioCompositionAndRecordings(DeleteTwilioRecordingsDto deleteTwilioRecordings)
        {
            await _twilioService.DeleteCompositionAndRecordings(deleteTwilioRecordings);
            return Result.Ok();
        }
    }
}
