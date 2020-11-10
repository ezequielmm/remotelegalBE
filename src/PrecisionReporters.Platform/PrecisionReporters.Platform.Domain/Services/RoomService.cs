using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Commons;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using FluentResults;
using PrecisionReporters.Platform.Domain.Errors;

namespace PrecisionReporters.Platform.Domain.Services
{
    public class RoomService : IRoomService
    {
        private readonly ITwilioService _twilioService;
        private readonly IRoomRepository _roomRepository;

        private readonly Random _random = new Random();

        public RoomService(ITwilioService twilioService, IRoomRepository roomRepository)
        {
            _twilioService = twilioService;
            _roomRepository = roomRepository;
        }

        public async Task<Result<Room>> Create(Room room)
        {
            var newRoom = await _roomRepository.Create(room);
            await _twilioService.CreateRoom(newRoom.Name);

            return Result.Ok(room);
        }

        public async Task<Result<Room>> GetByName(string roomName)
        {
            var matchingRooms = await _roomRepository.GetByFilter(x => x.Name == roomName);

            if ((matchingRooms?.Count ?? 0) == 0)
                return Result.Fail(new ResourceNotFoundError());

            return Result.Ok(matchingRooms.First());
        }

        public Result<string> GenerateRoomToken(string roomName)
        {
            if (string.IsNullOrEmpty(roomName))
                return Result.Fail<string>(new InvalidInputError("Room name can not be null or empty"));

            if (GetByName(roomName).Result == null)
                return Result.Fail(new ResourceNotFoundError($"The Room with name '{roomName}' doesn't exist"));

            // get user from jwt token or session
            var username = $"user-{_random.Next(20)}";
            var twilioToken = _twilioService.GenerateToken(roomName, username);
            return Result.Ok(twilioToken);
        }
    }
}
