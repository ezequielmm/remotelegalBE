using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class ParticipantDto
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        // TODO: use enum
        public string Role { get; set; }
        public bool? IsAdmitted { get; set; }
        public bool HasJoined { get; set; }
        public UserOutputDto User { get; set; }
        public bool IsMuted { get; set; }
        public DeviceInfoDto DeviceInfo { get; set; }
    }
}
