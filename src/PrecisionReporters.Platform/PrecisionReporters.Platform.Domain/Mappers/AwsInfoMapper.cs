using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using System;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class AwsInfoMapper : IMapper<AwsSessionInfo, AwsInfoDto, object>
    {
        public AwsInfoDto ToDto(AwsSessionInfo model)
        {
            throw new NotImplementedException();
        }

        public AwsSessionInfo ToModel(AwsInfoDto dto)
        {
            return new AwsSessionInfo
            {
                AvailabilityZone = dto.AvailabilityZone,
                ContainerId = dto.ContainerId
            };
        }

        public AwsSessionInfo ToModel(object dto)
        {
            throw new NotImplementedException();
        }
    }
}
