using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class AzureMediaServiceJob : BaseEntity<AzureMediaServiceJob>
    {
        /// <summary>
        /// The Job Id provided by Azure after registering a job.
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// The TwilioAudioRecording the job was requested for.
        /// </summary>
        public TwilioAudioRecording TwilioAudioRecording { get; set; }

        [ForeignKey(nameof(TwilioAudioRecording))]
        public Guid? TwilioAudioRecordingId { get; set; }

        public override void CopyFrom(AzureMediaServiceJob entity)
        {
            Id = entity.Id;
            CreationDate = entity.CreationDate;
            JobId = entity.JobId;
            TwilioAudioRecording.CopyFrom(TwilioAudioRecording);
            TwilioAudioRecordingId = entity.TwilioAudioRecordingId;
        }
    }
}
