using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class DeviceInfoMapper : IMapper<DeviceInfo, DeviceInfoDto, object>
    {
        public DeviceInfoDto ToDto(DeviceInfo model)
        {
            throw new System.NotImplementedException();
        }

        public DeviceInfo ToModel(DeviceInfoDto dto)
        {
            return new DeviceInfo
            {
                CameraName = dto.Camera.Name,
                CameraStatus = dto.Camera.Status,
                CreationDate = System.DateTime.UtcNow,
                MicrophoneName = dto.Microphone.Name,
                SpeakersName = dto.Speakers.Name
            };
        }

        public DeviceInfo ToModel(object dto)
        {
            throw new System.NotImplementedException();
        }
    }
}
