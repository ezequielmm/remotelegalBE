using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
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

        public RoomService(ITwilioService twilioService, IRoomRepository roomRepository)
        {
            _twilioService = twilioService;
            _roomRepository = roomRepository;
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

        public async Task<Result<string>> GenerateRoomToken(string roomName, User user, ParticipantType role, string email )
        {
            var room = await _roomRepository.GetFirstOrDefaultByFilter(x => x.Name == roomName );
            if (room == null)
                return Result.Fail(new ResourceNotFoundError($"Room {roomName} not found"));

            if (room.Status != RoomStatus.InProgress)
                return Result.Fail(new InvalidInputError($"There was an error ending the the Room '{room.Name}'. It's not in progress. Current state: {room.Status}"));

            var twilioIdentity = new TwilioIdentity
            {
                Name = $"{user.FirstName} {user.LastName}",
                Role = Enum.GetName(typeof(ParticipantType),role),
                Email = email
            };
            
            var twilioToken = _twilioService.GenerateToken(roomName, twilioIdentity);

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

                if (room.IsRecordingEnabled)
                {
                    //TODO Create a compostion for each RoomResource associated with the Room Name (twilio UniqueName)
                    var composition = await _twilioService.CreateComposition(room.SId, witnessEmail);
                    room.Composition = new Composition
                    {
                        SId = composition.Sid,
                        Status = CompositionStatus.Queued,
                        StartDate = DateTime.UtcNow,
                        Url = composition.Url.AbsoluteUri
                    };
                }

                await _roomRepository.Update(room);
            }
            else
            {
                return Result.Fail(new InvalidInputError($"There was an error ending the the Room '{room.Name}'. It's not in progress. Current state: {room.Status}"));
            }

            return Result.Ok(room);
        }

        public async Task<Result<Room>> StartRoom(Room room)
        {
            if (room.Status != RoomStatus.Created)
                return Result.Fail(new InvalidStatusError());

            try
            {
                room = await _twilioService.CreateRoom(room);
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
    }
}
