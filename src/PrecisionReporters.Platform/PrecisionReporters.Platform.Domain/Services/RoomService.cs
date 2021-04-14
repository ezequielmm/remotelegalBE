using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Dtos;
using PrecisionReporters.Platform.Domain.Errors;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using Twilio.Exceptions;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class RoomService : IRoomService
    {
        private readonly ITwilioService _twilioService;
        private readonly IRoomRepository _roomRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDepositionRepository _depositionRepository;

        public RoomService(
            ITwilioService twilioService,
            IRoomRepository roomRepository,
            IUserRepository userRepository,
            IDepositionRepository depositionRepository)
        {
            _twilioService = twilioService;
            _roomRepository = roomRepository;
            _userRepository = userRepository;
            _depositionRepository = depositionRepository;
        }

        public async Task<Result<Room>> Create(Room room)
        {
            var newRoom = await _roomRepository.Create(room);
            return Result.Ok(newRoom);
        }

        public async Task<Result<Room>> GetById(Guid roomId)
        {
            var room = await _roomRepository.GetFirstOrDefaultByFilter(x => x.Id == roomId, new[] { nameof(Room.Composition) });
            if (room == null)
                return Result.Fail(new ResourceNotFoundError());

            return Result.Ok(room);
        }

        public async Task<Result<Room>> GetByName(string roomName)
        {
            var matchingRooms = await _roomRepository.GetByFilter(x => x.Name == roomName, new[] { nameof(Room.Composition) });

            if ((matchingRooms?.Count ?? 0) == 0)
                return Result.Fail(new ResourceNotFoundError());

            return Result.Ok(matchingRooms.First());
        }

        public async Task<Result<string>> GenerateRoomToken(string roomName, User user, ParticipantType role, string email, ChatDto chatDto = null)
        {
            var room = await _roomRepository.GetFirstOrDefaultByFilter(x => x.Name == roomName);
            if (room == null)
                return Result.Fail(new ResourceNotFoundError($"Room {roomName} not found"));

            if (room.Status != RoomStatus.InProgress)
                return Result.Fail(new InvalidInputError($"There was an error ending the the Room '{room.Name}'. It's not in progress. Current state: {room.Status}"));

            var twilioIdentity = new TwilioIdentity
            {
                Name = $"{user.FirstName} {user.LastName}",
                Role = Enum.GetName(typeof(ParticipantType), role),
                Email = email
            };

            var grantChat = false;
            if (chatDto != null)
            {
                var result = await AddChatParticipant(chatDto, twilioIdentity, user);
                grantChat = result.IsSuccess;
            }


            var twilioToken = _twilioService.GenerateToken(roomName, twilioIdentity, grantChat);

            return Result.Ok(twilioToken);
        }

        public async Task<Result<Room>> EndRoom(Room room, string witnessEmail)
        {
            if (room.Status == RoomStatus.InProgress)
            {
                var roomResourceResult = await _twilioService.EndRoom(room);
                if (roomResourceResult.IsFailed)
                    return roomResourceResult.ToResult<Room>();

                room.EndDate = DateTime.UtcNow;
                room.Status = RoomStatus.Completed;
                
                await _roomRepository.Update(room);
            }
            else
            {
                return Result.Fail(new InvalidInputError($"There was an error ending the the Room '{room.Name}'. It's not in progress. Current state: {room.Status}"));
            }

            return Result.Ok(room);
        }

        public async Task<Result<Composition>> CreateComposition(Room room, string witnessEmail)
        {
            var compositionResource = await _twilioService.CreateComposition(room, witnessEmail);

            var composition = new Composition
            {
                SId = compositionResource?.Sid,
                Status = CompositionStatus.Queued,
                StartDate = DateTime.UtcNow,
                Url = compositionResource?.Url.AbsoluteUri,
                FileType = room.IsRecordingEnabled? ApplicationConstants.Mp4 : ApplicationConstants.Mp3
            };

            room.Composition = composition;
            await _roomRepository.Update(room);

            return Result.Ok(composition);
        }

        public async Task<Result<Room>> StartRoom(Room room, bool configureCallbacks)
        {
            if (room.Status != RoomStatus.Created)
                return Result.Fail(new InvalidStatusError());

            try
            {
                room = await _twilioService.CreateRoom(room, configureCallbacks);
                room.Status = RoomStatus.InProgress;
                room.StartDate = DateTime.UtcNow;

                // TODO: Review possible failures from repository, return Result<T>
                var updatedRoom = await _roomRepository.Update(room);
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
            var room = await _roomRepository.GetFirstOrDefaultByFilter(x => x.SId == roomSid);
            return Result.Ok(room);
        }

        public async Task<Result<Room>> Update(Room room)
        {
            var updatedRoom = await _roomRepository.Update(room);
            return Result.Ok(updatedRoom);
        }

        private async Task<Result> AddChatParticipant(ChatDto chatDto, TwilioIdentity twilioIdentity, User user)
        {
            if (chatDto.CreateChat && string.IsNullOrWhiteSpace(chatDto.SId))
            {
                var result = await _twilioService.CreateChat(chatDto.ChatName);
                chatDto.SId = result.Value;
                var deposition = await _depositionRepository.GetById(new Guid(chatDto.ChatName));
                deposition.ChatSid = chatDto.SId;
                await _depositionRepository.Update(deposition);
            }

            if (string.IsNullOrWhiteSpace(user.SId))
            {
                var result = await _twilioService.CreateChatUser(twilioIdentity);
                if (result.IsSuccess)
                {
                    user.SId = result.Value;
                    await _userRepository.Update(user);
                }
            }

            if (chatDto.AddParticipant)
            {
                await _twilioService.AddUserToChat(chatDto.SId, twilioIdentity, user.SId);
            }

            return Result.Ok();
        }
    }
}
