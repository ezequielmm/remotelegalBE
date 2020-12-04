using FluentResults;
using PrecisionReporters.Platform.Data.Entities;
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

        public async Task<Result<string>> GenerateRoomToken(string roomName, string identity)
        {
            var getRoomResult = await GetByName(roomName);
            if (getRoomResult.IsFailed)
            {
                // TODO: Investigate how to obtain a result based on a previous operation. (JD)
                return Result.Fail(getRoomResult.Errors.First());
            }

            var room = getRoomResult.Value;
            if (room.Status != RoomStatus.InProgress)
                return Result.Fail(new InvalidInputError($"There was an error ending the the Room '{room.Name}'. It's not in progress. Current state: {room.Status}"));

            var twilioToken = _twilioService.GenerateToken(roomName, identity);

            return Result.Ok(twilioToken);
        }

        public async Task<Result<Room>> EndRoom(Room room)
        {
            if (room.Status == RoomStatus.InProgress)
            {
                var roomResource = await _twilioService.EndRoom(room.SId);

                room.EndDate = DateTime.UtcNow;
                room.Status = RoomStatus.Completed;

                if (room.IsRecordingEnabled)
                {
                    var composition = await _twilioService.CreateComposition(roomResource);
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
            var updatedRoom = room;
            if (room.Status != RoomStatus.Created)
                return Result.Fail(new InvalidStatusError());

            try
            {
                var resourceRoom = await _twilioService.CreateRoom(room);
                room.SId = resourceRoom.Sid;
                room.Status = RoomStatus.InProgress;
                room.StartDate = DateTime.UtcNow;

                // TODO: Review possible failures from repository, return Result<T>
                updatedRoom = await _roomRepository.Update(room);
            }
            catch (ApiException ex) when (ex.Message == ApplicationConstants.RoomExistError)
            {
                //We shouldn't throw an exception if room is already started.
            }

            return Result.Ok(updatedRoom);
        }

        public async Task<Result<Room>> GetRoomBySId(string roomSid)
        {
            var room = await _roomRepository.GetFirstOrDefaultByFilter(x => x.SId == roomSid);
            return Result.Ok(room);
        }
    }
}
