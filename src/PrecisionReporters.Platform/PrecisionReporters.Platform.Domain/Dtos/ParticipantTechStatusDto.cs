using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class ParticipantTechStatusDto
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool? IsAdmitted { get; set; }
        public bool HasJoined { get; set; }
        public string Device { get; set; }
        public string Browser { get; set; }
        public string OperatingSystem { get; set; }
        public bool IsMuted { get; set; }
        public string IP { get; set; }
        public DeviceInfoDto Devices { get; set; }
    }
}
