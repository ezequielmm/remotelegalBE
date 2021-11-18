namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class ParticipantValidationDto
    {
        public bool IsUser { get; set; }
        public bool IsVerify { get; set; }
        public ParticipantDto Participant { get; set; }
    }
}
