namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class JoinDepositionDto
    {
        public string WitnessEmail { get; set; }
        public string Token { get; set; }
        public string TimeZone { get; set; }
        public bool IsOnTheRecord { get; set; }
    }
}
