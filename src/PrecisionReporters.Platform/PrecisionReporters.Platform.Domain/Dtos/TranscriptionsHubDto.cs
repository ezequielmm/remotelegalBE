using MessagePack;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    [MessagePackObject]
    public class TranscriptionsHubDto
    {
        [Key("depositionId")]
        public string DepositionId { get; set; }
        [Key("sampleRate")]
        public int SampleRate { get; set; }
        [Key("audio")]
        public byte[] Audio { get; set; }
    }
}