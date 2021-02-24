using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

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
        DraftTranscription
    }
}
