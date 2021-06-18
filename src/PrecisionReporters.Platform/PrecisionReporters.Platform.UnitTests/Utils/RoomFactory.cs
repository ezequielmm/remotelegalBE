using PrecisionReporters.Platform.Data.Entities;
using System;
using PrecisionReporters.Platform.Domain.Dtos;

namespace PrecisionReporters.Platform.UnitTests.Utils
{
    public static class RoomFactory
    {
        public static Room GetRoomById(Guid roomId)
        {
            return new Room
            {
                Id = roomId,
                CreationDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(5),
                IsRecordingEnabled = false,
                Name = "RoomTest",
                Status = RoomStatus.Created,
                Composition = new Composition
                {
                    RoomId = roomId
                },
                SId = "12345"
            };
        }

        public static Room GetRoomByName(string roomName)
        {
            return new Room
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(5),
                IsRecordingEnabled = false,
                Name = roomName,
                Status = RoomStatus.Created,
                SId = "12345"
            };
        }

        public static Room GetRoomWithInProgressStatus()
        {
            return new Room
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(5),
                IsRecordingEnabled = false,
                Name = "RoomTest",
                Status = RoomStatus.InProgress,
                SId = "12345"
            };
        }

        public static CreateRoomDto GetCreateRoomDto()
        {
            return new CreateRoomDto
            {
                Name = "Mock Create Room",
                IsRecordingEnabled = false
            };
        }

        public static RoomDto GetRoomDto()
        {
            return new RoomDto
            {
                Name = "Mock Room Dto",
                IsRecordingEnabled = true
            };
        }
    }
}
