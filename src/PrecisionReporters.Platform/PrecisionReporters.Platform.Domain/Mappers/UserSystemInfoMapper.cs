using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class UserSystemInfoMapper : IMapper<UserSystemInfo, UserSystemInfoDto, object>
    {
        public UserSystemInfoDto ToDto(UserSystemInfo model)
        {
            throw new System.NotImplementedException();
        }

        public UserSystemInfo ToModel(UserSystemInfoDto dto)
        {
            return new UserSystemInfo
            {
                OS = dto.OS,
                Browser = dto.Browser,
                Device = dto.Device
            };
        }

        public UserSystemInfo ToModel(object dto)
        {
            throw new System.NotImplementedException();
        }
    }
}
