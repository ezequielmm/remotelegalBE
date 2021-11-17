using PrecisionReporters.Platform.Data.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace PrecisionReporters.Platform.Data.Entities
{
    /// <summary>
    /// See https://gist.github.com/igracia/581a8dfb8b88aa624c1d456adc6affb2 for
    /// more ffprobe properties.
    /// </summary>
    public class TwilioAudioRecording : BaseEntity<TwilioAudioRecording>
    {
        /// <summary>
        /// The Recording SID property from twilio.
        /// </summary>
        public string RecordingSId { get; set; }

        /// <summary>
        /// The Room SID property from Twilio.
        /// </summary>
        public string RoomSId { get; set; }

        /// <summary>
        /// The Date property from Twilio (it doesn't have miliseconds precision).
        /// </summary>
        [Column(TypeName = "datetime(3)")]
        public DateTime? RecordingCreationDateTime { get; set; }

        /// <summary>
        /// The Duration property from Twilio (it doesn't have miliseconds precision).
        /// </summary>
        [Column(TypeName = "time(3)")]
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// The creation_time property from ffProbe.
        /// </summary>
        [Column(TypeName = "datetime(3)")]
        public DateTime? FfProbeCreationDateTime { get; set; }

        /// <summary>
        /// The Duration property got from ffProbe.
        /// </summary>
        [Column(TypeName = "time(3)")]
        public TimeSpan? FfProbeDuration { get; set; }

        /// <summary>
        /// The start_time property got from ffProbe.
        /// </summary>
        [Column(TypeName = "time(3)")]
        public TimeSpan? FfProbeStartTime { get; set; }

        /// <summary>
        /// The current status of the transcription for this recording.
        /// </summary>
        public RecordingTranscriptionStatus TranscriptionStatus { get; set; }

        /// <summary>
        /// Collection navigation property.
        /// </summary>
        public List<AzureMediaServiceJob> AzureMediaServiceJobs { get; set; }

        /// <summary>
        /// Collection navigation property.
        /// </summary>
        public List<PostProcessTranscription> PostProcessTranscriptions { get; set; }

        public override void CopyFrom(TwilioAudioRecording entity)
        {
            Id = entity.Id;
            CreationDate = entity.CreationDate;
            RecordingSId = entity.RecordingSId;
            RoomSId = entity.RoomSId;
            RecordingCreationDateTime = entity.RecordingCreationDateTime;
            Duration = entity.Duration;
            FfProbeCreationDateTime = entity.FfProbeCreationDateTime;
            FfProbeDuration = entity.FfProbeDuration;
            FfProbeStartTime = entity.FfProbeStartTime;
            TranscriptionStatus = entity.TranscriptionStatus;
            AzureMediaServiceJobs = entity.AzureMediaServiceJobs
                .Select(x =>
                {
                    var azureMediaServiceJob = new AzureMediaServiceJob();
                    azureMediaServiceJob.CopyFrom(x);
                    return azureMediaServiceJob;
                })
                .ToList();
            PostProcessTranscriptions = entity.PostProcessTranscriptions
                .Select(x =>
                {
                    var postProcessTranscription = new PostProcessTranscription();
                    postProcessTranscription.CopyFrom(x);
                    return postProcessTranscription;
                })
                .ToList();
        }
    }
}
