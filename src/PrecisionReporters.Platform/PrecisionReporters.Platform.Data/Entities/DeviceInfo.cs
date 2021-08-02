using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Data.Entities
{
    public class DeviceInfo : BaseEntity<DeviceInfo>
    {
        public string CameraName { get; set; }
        public CameraStatus? CameraStatus { get; set; }
        public string MicrophoneName { get; set; }
        public string SpeakersName { get; set; }

        public override void CopyFrom(DeviceInfo entity)
        {
            CameraName = entity.CameraName;
            MicrophoneName = entity.MicrophoneName;
            CameraStatus = entity.CameraStatus;
            SpeakersName = entity.SpeakersName;
        }
    }
}
