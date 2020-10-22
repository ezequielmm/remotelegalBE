using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Repositories.Interfaces;
using PrecisionReporters.Platform.Domain.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<Room> Create(Room room)
        {
            var newRoom = await _roomRepository.Create(room);
            await _twilioService.CreateRoom(newRoom.Name);

            return room;
        }

        public async Task<Room> GetByName(string roomName)
        {
            return (await _roomRepository.GetByFilter(x => x.Name == roomName)).FirstOrDefault();
        }

        public string GenerateRoomToken(string roomName)
        {
            if (String.IsNullOrEmpty(roomName))
            {
                throw new ArgumentNullException("Room name can not be null or empty");
            }

            if (GetByName(roomName).Result == null)
            {
                throw new ArgumentException($"The Room with name '{roomName}' doesn't exist");
            }

            // get user from jwt token or session
            var username = $"user-{_random.Next(20)}";
            return _twilioService.GenerateToken(roomName, username);
        }
    }
}
