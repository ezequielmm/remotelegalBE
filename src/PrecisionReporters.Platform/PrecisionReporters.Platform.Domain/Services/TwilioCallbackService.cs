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
using PrecisionReporters.Platform.Shared.Errors;

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
            _logger.LogInformation("Handling Twilio's Room Status callback, RoomSId: {0}, Event: {1}", roomEvent.RoomSid, roomEvent.StatusCallbackEvent);

            try
            {
                var status = EnumHandler.GetEnumValue<RoomStatusCallback>(roomEvent.StatusCallbackEvent);

                var roomResult = await _roomService.GetRoomBySId(roomEvent.RoomSid);
                if (roomResult == null)
                {
                    _logger.LogInformation("There was an error trying to get room with SId: {0}.", roomEvent.RoomSid);
                    return Result.Fail(new ResourceNotFoundError("Room not found"));
                }

                var room = roomResult.Value;

                switch (status)
                {
                    case RoomStatusCallback.RoomEnded:
                        {
                            var depositionResult = await _depositionService.GetDepositionByRoomId(room.Id);
                            if (depositionResult.IsFailed)
                            {
                                _logger.LogInformation("Deposition of room id: {0} not found", room.Id);
                                return depositionResult.ToResult<Room>();
                            }

                            var witness = depositionResult.Value.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
                            //TODO: If the Deposition never went OnRecord, we don't have to create a composition

                            _logger.LogInformation($"{nameof(TwilioCallbackService)}.{nameof(TwilioCallbackService.UpdateStatusCallback)} Create COMPOSIION event Room Sid: {room?.SId}, Witness Email: {witness?.Email}");

                            await _roomService.CreateComposition(room, witness?.Email);

                            return Result.Ok();
                        }
                    case RoomStatusCallback.ParticipantDisconnected:
                        {
                            //Verify if this room is a break room for remove attendees
                            var breakRoom = await _breakRoomService.GetByRoomId(room.Id);
                            if (breakRoom.IsSuccess)
                            {
                                await _breakRoomService.RemoveAttendeeCallback(breakRoom.Value, roomEvent.ParticipantIdentity);
                            }
                            return Result.Ok();
                        }
                    default:
                        return Result.Ok();
                }
            }
            catch (ArgumentException e)
            {
                _logger.LogInformation(e,"Skipping Room Status Callback event {0}", roomEvent.StatusCallbackEvent);
                return Result.Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e,"Error handling Twilio's room callback {0}",e.Message);
                return Result.Fail(new Error(e.Message));
            }
        }
    }
}
