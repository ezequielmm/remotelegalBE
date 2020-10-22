using System;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class RoomDto
    {
        public string Name { get; set; }

        public Guid Id { get; set; }

        public DateTime CreationDate { get; set; }
    }
}
