using FluentResults;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Handlers;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TwilioCallbackService : ITwilioCallbackService
    {
        private readonly IDepositionService _depositionService;
        private readonly IRoomService _roomService;
        private readonly IBreakRoomService _breakRoomService;
        private readonly ILogger<TwilioCallbackService> _logger;

        public TwilioCallbackService(IDepositionService depositionService,
            IRoomService roomService,
            ILogger<TwilioCallbackService> logger, IBreakRoomService breakRoomService)
        {
            _depositionService = depositionService;
            _roomService = roomService;
            _logger = logger;
            _breakRoomService = breakRoomService;
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

                switch (status)
                {
                    case RoomStatusCallback.RoomEnded:
                        {
                            var depositionResult = await _depositionService.GetDepositionByRoomId(room.Id);
                            if (depositionResult.IsFailed)
                                return depositionResult.ToResult<Room>();

                            var witness = depositionResult.Value.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
                            //TODO: If the Deposition never went OnRecord, we don't have to create a composition
                            await _roomService.CreateComposition(room, witness.Email);

                            return Result.Ok();
                        }
                    case RoomStatusCallback.ParticipantDisconnected:
                        {
                            //Verify if this room is a break room for remove attende
                            var breakRoom = await _breakRoomService.GetByRoomId(room.Id);
                            if (breakRoom.IsSuccess)
                            {
                                var result = await _breakRoomService.RemoveAttendeeCallback(breakRoom.Value, roomEvent.ParticipantIdentity);
                            }
                            return Result.Ok();
                        }
                    default:
                        return Result.Ok();
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
