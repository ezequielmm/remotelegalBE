using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Domain.Dtos;
using System.Linq;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class ParticipantTechStatusMapper : IMapper<Participant, ParticipantTechStatusDto, object>
    {
        public ParticipantTechStatusDto ToDto(Participant model)
        {
            var lastSystemInfo = model.User.ActivityHistories?.Where(t => t.Action == ActivityHistoryAction.SetSystemInfo)
                .OrderByDescending(d => d.ActivityDate).FirstOrDefault();

            return new ParticipantTechStatusDto
            {
                Id = model.Id,
                CreationDate = model.CreationDate,
                Email = model.Email,
                Name = model.Name,
                Role = model.Role.ToString(),
                Browser = lastSystemInfo!=null? lastSystemInfo.Browser:null,
                Device = lastSystemInfo != null ? lastSystemInfo.Device:null,
                OperatingSystem = lastSystemInfo != null ? lastSystemInfo.OperatingSystem:null,
                IP = lastSystemInfo != null ? lastSystemInfo.IPAddress:null,
                IsMuted = model.IsMuted,
                IsAdmitted = model.IsAdmitted,
                HasJoined = model.HasJoined,
                Devices = model.DeviceInfo != null ?
                    new DeviceInfoDto
                    {
                        Camera = new CameraDto { Name = model.DeviceInfo.CameraName, Status = model.DeviceInfo.CameraStatus },
                        Microphone = new MicrophoneDto { Name = model.DeviceInfo.MicrophoneName },
                        Speakers = new SpeakersDto { Name = model.DeviceInfo.SpeakersName }
                    }
                    : null
            };
        }

        public Participant ToModel(ParticipantTechStatusDto dto)
        {
            throw new System.NotImplementedException();
        }

        public Participant ToModel(object dto)
        {
            throw new System.NotImplementedException();
        }
    }
}
