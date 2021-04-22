using System.ComponentModel;

namespace PrecisionReporters.Platform.Data.Enums
{
    public enum DocumentType
    {   
        [Description("exhibits")]
        Exhibit,
        [Description("captions")]
        Caption,
        [Description("transcriptions")]
        Transcription,
        [Description("transcriptions")]
        DraftTranscription,
        [Description("transcriptions")]
        DraftTranscriptionWord
    }
}
