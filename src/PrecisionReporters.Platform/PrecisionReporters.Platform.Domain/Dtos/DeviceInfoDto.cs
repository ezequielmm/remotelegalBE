namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class DeviceInfoDto
    {
        public CameraDto Camera { get; set; }
        public MicrophoneDto Microphone { get; set; }
        public SpeakersDto Speakers { get; set; }
    }
}
