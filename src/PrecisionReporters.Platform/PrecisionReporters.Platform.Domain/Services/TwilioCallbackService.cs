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
using PrecisionReporters.Platform.Data.Repositories.Interfaces;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class TwilioCallbackService : ITwilioCallbackService
    {
        private readonly IDepositionService _depositionService;
        private readonly IRoomService _roomService;
        private readonly IBreakRoomService _breakRoomService;
        private readonly ILogger<TwilioCallbackService> _logger;
        private readonly ITwilioService _twilioService;
        private readonly ITwilioParticipantRepository _twilioParticipantRepository;

        public TwilioCallbackService(IDepositionService depositionService,
            IRoomService roomService,
            ILogger<TwilioCallbackService> logger,
            IBreakRoomService breakRoomService,
            ITwilioService twilioService,
            ITwilioParticipantRepository twilioParticipantRepository)
        {
            _depositionService = depositionService;
            _roomService = roomService;
            _logger = logger;
            _breakRoomService = breakRoomService;
            _twilioService = twilioService;
            _twilioParticipantRepository = twilioParticipantRepository;
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
                                _logger.LogInformation("RoomEnded - Deposition of room id: {0} not found", room.Id);
                                return depositionResult.ToResult<Room>();
                            }

                            var witness = depositionResult.Value.Participants.FirstOrDefault(x => x.Role == ParticipantType.Witness);
                            //TODO: If the Deposition never went OnRecord, we don't have to create a composition

                            _logger.LogInformation($"{nameof(TwilioCallbackService)}.{nameof(TwilioCallbackService.UpdateStatusCallback)} Create COMPOSIION event Room Sid: {room?.SId}, Witness Email: {witness?.Email}");

                            await _roomService.CreateComposition(room, witness?.Email, depositionResult.Value.Id);

                            return Result.Ok();
                        }
                    case RoomStatusCallback.ParticipantDisconnected:
                        {
                            //Verify if this room is a break room for remove attendees
                            var breakRoom = await _breakRoomService.GetByRoomId(room.Id);
                            if (breakRoom.IsSuccess)
                            {
                                await _breakRoomService.RemoveAttendeeCallback(breakRoom.Value, roomEvent.ParticipantIdentity);
                                return Result.Ok();
                            }

                            var twilioParticipantToUpdate = await _twilioParticipantRepository.GetFirstOrDefaultByFilter(x => x.ParticipantSid == roomEvent.ParticipantSid);
                            if (twilioParticipantToUpdate != null)
                            {
                                twilioParticipantToUpdate.DisconnectTime = roomEvent.Timestamp.UtcDateTime;
                                await _twilioParticipantRepository.Update(twilioParticipantToUpdate);
                            }

                            return Result.Ok();
                        }
                    case RoomStatusCallback.ParticipantConnected:
                        {
                            var depositionResult = await _depositionService.GetDepositionByRoomId(room.Id);
                            if (depositionResult.IsFailed)
                            {
                                _logger.LogInformation("ParticipantConnected - Deposition of room id: {0} not found", room.Id);
                                return Result.Ok();
                            }

                            var currentParticipant = depositionResult?.Value?.Participants?.FirstOrDefault(p => p.Email == _twilioService.DeserializeObject(roomEvent.ParticipantIdentity)?.Email);
                            if (currentParticipant == null)
                            {
                                _logger.LogInformation("ParticipantConnected - Participant not found for deposition ID: {0}, Participant Email {1, Participant SID {2}}",
                                    depositionResult.Value.Id, _twilioService.DeserializeObject(roomEvent.ParticipantIdentity)?.Email, roomEvent.ParticipantSid);
                                return Result.Ok();
                            }

                            var twilioParticipant = new TwilioParticipant { Participant = currentParticipant, ParticipantSid = roomEvent.ParticipantSid, JoinTime = roomEvent.Timestamp.UtcDateTime };
                            await _twilioParticipantRepository.Create(twilioParticipant);
                            return Result.Ok();
                        }
                    default:
                        return Result.Ok();
                }
            }
            catch (ArgumentException e)
            {
                _logger.LogInformation(e, "Skipping Room Status Callback event {0}", roomEvent.StatusCallbackEvent);
                return Result.Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error handling Twilio's room callback {0}", e.Message);
                return Result.Fail(new Error(e.Message));
            }
        }
    }
}
