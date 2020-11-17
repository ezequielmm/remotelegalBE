using System;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class RoomDto
    {
        public string Name { get; set; }

        public Guid Id { get; set; }

        public DateTime CreationDate { get; set; }

        public bool IsRecordingEnabled { get; set; }

        // TODO: replace this string for the Status Enum
        public string Status { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public CompositionDto Composition { get; set; }
    }
}
