using MessagePack;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    [MessagePackObject]
    public class TranscriptionsChangeStatusDto
    {
        [Key("offRecord")]
        public bool OffRecord { get; set; }
    }
}