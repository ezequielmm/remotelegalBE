using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class PostProcessTranscription : BaseEntity<PostProcessTranscription>
    {
        /// <summary>
        /// Text of this transcription.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The TwilioAudioRecording this transcription belongs to.
        /// </summary>
        public TwilioAudioRecording TwilioAudioRecording { get; set; }

        [ForeignKey(nameof(TwilioAudioRecording))]
        public Guid? TwilioAudioRecordingId { get; set; }

        /// <summary>
        /// Datetime of the beginning of the transcription.
        /// </summary>
        [Column(TypeName = "datetime(3)")]
        public DateTime TranscriptDateTime { get; set; }

        /// <summary>
        /// Start time relative to the beginning of the TwilioAudioRecording.
        /// </summary>
        [Column(TypeName = "time(3)")]
        public TimeSpan TwilioAudioRecordingStartTime { get; set; }

        /// <summary>
        /// Start time relative to the beginning of the composition.
        /// </summary>
        [Column(TypeName = "time(3)")]
        public TimeSpan CompositionStartTime { get; set; }

        /// <summary>
        /// Duration of the audio interval that generated this transcription.
        /// </summary>
        [Column(TypeName = "time(3)")]
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Confidence of this transcription.
        /// </summary>
        public double Confidence { get; set; }

        public override void CopyFrom(PostProcessTranscription entity)
        {
            Id = entity.Id;
            CreationDate = entity.CreationDate;
            Text = entity.Text;
            TwilioAudioRecording.CopyFrom(entity.TwilioAudioRecording);
            TwilioAudioRecordingId = entity.TwilioAudioRecordingId;
            TranscriptDateTime = entity.TranscriptDateTime;
            TwilioAudioRecordingStartTime = entity.TwilioAudioRecordingStartTime;
            CompositionStartTime = entity.CompositionStartTime;
            Duration = entity.Duration;
        }
    }
}
