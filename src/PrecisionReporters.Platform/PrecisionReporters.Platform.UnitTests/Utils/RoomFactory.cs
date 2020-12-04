using PrecisionReporters.Platform.Data.Entities;
using System;

namespace PrecisionReporters.Platform.UnitTests.Utils
{
    public class RoomFactory
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
    }
}
