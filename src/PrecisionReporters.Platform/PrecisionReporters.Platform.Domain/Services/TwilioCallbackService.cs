using System;
using System.Threading.Tasks;
using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Extensions;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Handlers;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TwilioCallbackService : ITwilioCallbackService
    {
        private readonly IDepositionService _depositionService;
        private readonly IRoomService _roomService;
        private readonly ILogger<TwilioCallbackService> _logger;

        public TwilioCallbackService(IDepositionService depositionService, 
            IRoomService roomService,
            ILogger<TwilioCallbackService> logger) 
        {
            _depositionService = depositionService;
            _roomService = roomService;
            _logger = logger;
        }

        public async Task<Result<Room>> UpdateStatusCallback(RoomCallbackDto roomEvent)
        {
            _logger.LogDebug($"Handling Twilio's Room Status callback, RoomSId: {roomEvent.RoomSid}, Event: {roomEvent.StatusCallbackEvent}");
    
            try 
            {
                var status = EnumHandler.GetEnumValue<RoomStatusCallback>(roomEvent.StatusCallbackEvent);

                var roomResult = await _roomService.GetRoomBySId(roomEvent.RoomSid);
                if (roomResult.IsFailed)
                    return roomResult;

                var room = roomResult.Value;

                switch(status)
                {
                    case RoomStatusCallback.RecordingCompleted:
                    {
                        if (room.StartedReference == roomEvent.ParticipantSid)
                        {
                            room.RecordingStartDate = roomEvent.Timestamp.UtcDateTime.AddSeconds(- roomEvent.Duration);
                            await _roomService.Update(room);
                        }
                        return Result.Ok();
                    }
                    case RoomStatusCallback.ParticipantConnected:
                    {
                        if (room.StartedReference == null)
                        {
                            room.StartedReference = roomEvent.ParticipantSid;
                            await _roomService.Update(room);
                        }
                        return Result.Ok();
                    }   
                    case RoomStatusCallback.RoomEnded:
                    {
                        var depositionResult = await _depositionService.GetDepositionByRoomId(room.Id);
                        if (depositionResult.IsFailed)
                            return depositionResult.ToResult<Room>();

                        if (depositionResult.Value.Status != DepositionStatus.Completed)
                            await _depositionService.EndDeposition(depositionResult.Value.Id);
                            
                        return Result.Ok();
                    }
                    default: 
                        return Result.Fail(new InvalidInputError("Invalid recording status."));
                }
                
            }
            catch (ArgumentException e) 
            {
                _logger.LogDebug($"Skipping Room Status Callback event {roomEvent.StatusCallbackEvent}");
                return Result.Ok();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error handling Twilio's room callback {e.Message}");
                return Result.Fail(new Error(e.Message));
            }
        }
    }
}
