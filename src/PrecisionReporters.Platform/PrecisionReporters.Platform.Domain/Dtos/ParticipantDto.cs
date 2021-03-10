using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class ParticipantDto
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        // TODO: use enum
        public string Role { get; set; }
        public UserOutputDto User { get; set; }
    }
}
