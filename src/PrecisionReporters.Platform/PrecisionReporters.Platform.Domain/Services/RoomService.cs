﻿using FluentResults;
using Microsoft.Extensions.Logging;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using PrecisionReporters.Platform.Shared.Commons;
using PrecisionReporters.Platform.Shared.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio.Exceptions;
using Twilio.Rest.Video.V1;
using static Twilio.Rest.Video.V1.RoomResource;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class RoomService : IRoomService
    {
        private readonly ITwilioService _twilioService;
        private readonly IRoomRepository _roomRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDepositionRepository _depositionRepository;
        private readonly ILogger<RoomService> _logger;
        private readonly ITwilioParticipantRepository _twilioParticipantRepository;

        public RoomService(
            ITwilioService twilioService,
            IRoomRepository roomRepository,
            IUserRepository userRepository,
            IDepositionRepository depositionRepository,
            ILogger<RoomService> logger,
            ITwilioParticipantRepository twilioParticipantRepository)
        {
            _twilioService = twilioService;
            _roomRepository = roomRepository;
            _userRepository = userRepository;
            _depositionRepository = depositionRepository;
            _logger = logger;
            _twilioParticipantRepository = twilioParticipantRepository;
        }

        public async Task<Result<Room>> Create(Room room)
        {
            var newRoom = await _roomRepository.Create(room);
            return Result.Ok(newRoom);
        }

        public async Task<Result<Room>> GetById(Guid roomId)
        {
            var room = await _roomRepository.GetFirstOrDefaultByFilter(x => x.Id == roomId, new[] { nameof(Room.Composition) }).ConfigureAwait(false);
            if (room == null)
                return Result.Fail(new ResourceNotFoundError());

            return Result.Ok(room);
        }

        public async Task<Result<Room>> GetByName(string roomName)
        {
            var matchingRooms = await _roomRepository.GetByFilter(x => x.Name == roomName, new[] { nameof(Room.Composition) }).ConfigureAwait(false);

            if ((matchingRooms?.Count ?? 0) == 0)
                return Result.Fail(new ResourceNotFoundError());

            return Result.Ok(matchingRooms.First());
        }

        public async Task<Result<string>> GenerateRoomToken(string roomName, User user, ParticipantType role, string email, Participant participant)
        {
            var room = await _roomRepository.GetFirstOrDefaultByFilter(x => x.Name == roomName).ConfigureAwait(false);
            _logger.LogInformation($"{nameof(RoomService)}.{nameof(RoomService.GenerateRoomToken)} Room SID: {room?.SId}");
            if (room == null)
                return Result.Fail(new ResourceNotFoundError($"Room {roomName} not found"));

            if (room.Status != RoomStatus.InProgress)
                return Result.Fail(new InvalidInputError($"There was an error ending the the Room '{room?.Name}'. It's not in progress. Current state: {room?.Status}"));

            var twilioIdentity = new TwilioIdentity
            {
                FirstName = participant.Name ?? user.FirstName,
                LastName = participant.LastName ?? user.LastName,
                Role = (int) role,
                Email = email,
                IsAdmin = user.IsAdmin? 1 : 0
            };

            _logger.LogInformation($"{nameof(RoomService)}.{nameof(RoomService.GenerateRoomToken)} User Email: {email}");
            

            var twilioToken = _twilioService.GenerateToken(roomName, twilioIdentity);
            _logger.LogInformation($"{nameof(RoomService)}.{nameof(RoomService.GenerateRoomToken)} Twilio Token: {twilioToken}");

            return Result.Ok(twilioToken);
        }

        public async Task<Result<Room>> EndRoom(Room room, string witnessEmail)
        {
            _logger.LogInformation(string.Format("Closing Room Sid \"{0}\"- status {1}", room.SId, room.Status));
            if (room.Status == RoomStatus.InProgress)
            {
                var roomResourceResult = await _twilioService.EndRoom(room).ConfigureAwait(false);
                if (roomResourceResult.IsFailed)
                    return roomResourceResult.ToResult<Room>();
                
                room.EndDate = DateTime.UtcNow;
                room.Status = RoomStatus.Completed;
                _logger.LogInformation(string.Format("Updating Room Id: \"{0}\" - EndDate {1}, Status {2}", room.Id, room.EndDate, room.Status));
                await _roomRepository.Update(room);
                _logger.LogInformation(string.Format("Room Updated \"{0}\" - EndDate {1}, Status {2}", room.Id, room.EndDate, room.Status));
            }
            else
            {
                return Result.Fail(new InvalidInputError($"There was an error ending the the Room '{room.Name}'. It's not in progress. Current state: {room.Status}"));
            }

            return Result.Ok(room);
        }

        public async Task<Result<Composition>> CreateComposition(Room room, string witnessEmail, Guid depositionId)
        {
            var witnessParticipants = await _twilioParticipantRepository.GetByFilter(t => t.Participant.Email == witnessEmail && t.Participant.DepositionId.HasValue && t.Participant.DepositionId.Value == depositionId).ConfigureAwait(false);
            var twilioParticipantsSIDs = await _twilioService.GetWitnessSid(room.SId, witnessEmail).ConfigureAwait(false);

            if (!witnessParticipants.Any())
            {
                _logger.LogError($"{nameof(RoomService)}.{nameof(RoomService.CreateComposition)} Witness Email {witnessEmail}, RoomSid {room.SId}: User with Witness Email not found on DB");
                throw new Exception("Witness not found");
            }
            var lstWitnessSIDs = witnessParticipants.Select(p => p.ParticipantSid).ToArray();

            var differences = twilioParticipantsSIDs.Where(x => !lstWitnessSIDs.Contains(x));

            if (differences.Any())
                _logger.LogWarning($"{nameof(RoomService)}.{nameof(RoomService.CreateComposition)} - " + "Twilio's participant list {@0} - DB paticipant list {@1} - Differences between Twilio and BD list {@2}", twilioParticipantsSIDs, lstWitnessSIDs, differences);

            var compositionResource = await _twilioService.CreateComposition(room, lstWitnessSIDs);
            _logger.LogInformation($"{nameof(RoomService)}.{nameof(RoomService.CreateComposition)} Twilio Composition Created: {compositionResource.Sid}");
            var composition = new Composition
            {
                SId = compositionResource?.Sid,
                Status = CompositionStatus.Queued,
                StartDate = DateTime.UtcNow,
                Url = compositionResource?.Url.AbsoluteUri,
                FileType = room.IsRecordingEnabled ? ApplicationConstants.Mp4 : ApplicationConstants.Mp3
            };

            room.Composition = composition;
            await _roomRepository.Update(room);
            _logger.LogInformation($"{nameof(RoomService)}.{nameof(RoomService.CreateComposition)} BD Composition Created: {composition.Id}");
            return Result.Ok(composition);
        }

        public async Task<Result<Room>> StartRoom(Room room, bool configureCallbacks)
        {
            try
            {
                _logger.LogInformation($"{nameof(RoomService)}.{nameof(RoomService.StartRoom)} Room Name: {room?.Name}");
                room = await _twilioService.CreateRoom(room, configureCallbacks).ConfigureAwait(false);
                room.Status = RoomStatus.InProgress;
                room.StartDate = DateTime.UtcNow;

                _logger.LogInformation($"{nameof(RoomService)}.{nameof(RoomService.StartRoom)} Room Sid: {room?.SId}");
                // TODO: Review possible failures from repository, return Result<T>
                var updatedRoom = await _roomRepository.Update(room);

                _logger.LogInformation($"{nameof(RoomService)}.{nameof(RoomService.StartRoom)} Room Updated: {room?.Id}");
                return Result.Ok(updatedRoom);
            }
            catch (ApiException ex) when (ex.Message == ApplicationConstants.RoomExistError)
            {
                //We shouldn't throw an exception if room is already started.
            }

            return Result.Ok(room);
        }

        public async Task<Result<Room>> GetRoomBySId(string roomSid)
        {
            var room = await _roomRepository.GetFirstOrDefaultByFilter(x => x.SId == roomSid).ConfigureAwait(false);
            return Result.Ok(room);
        }

        public async Task<Result<Room>> Update(Room room)
        {
            var updatedRoom = await _roomRepository.Update(room);
            return Result.Ok(updatedRoom);
        }        


        public async Task<List<RoomResource>> GetTwilioRoomByNameAndStatus(string uniqueName, RoomStatusEnum status)
        {
            return await _twilioService.GetRoomsByUniqueNameAndStatus(uniqueName, status);
        }

        public async Task<bool> RemoveRecordingRules(string roomSid)
        {
            return await _twilioService.RemoveRecordingRules(roomSid).ConfigureAwait(false);
        }

        public async Task<bool> AddRecordingRules(string roomSid, TwilioIdentity witnessIdentity, bool IsVideoRecordingNeeded)
        {
            return await _twilioService.AddRecordingRules(roomSid, witnessIdentity, IsVideoRecordingNeeded).ConfigureAwait(false);
        }

        public async Task<string> RefreshRoomToken(Participant participant, Deposition deposition)
        {           
            var twilioToken = await GenerateRoomToken(deposition.Room.Name, participant.User, participant.Role, participant.Email, participant);


            return twilioToken.Value;
        }
    }
}
