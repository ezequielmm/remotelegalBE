using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Api.Dtos;
using System;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class VerifyUserMapper : IMapper<VerifyUser, object, CreateVerifyUserDto>
    {
        public object ToDto(VerifyUser model)
        {
            throw new NotImplementedException();
        }

        public VerifyUser ToModel(object dto)
        {
            throw new NotImplementedException();
        }

        public VerifyUser ToModel(CreateVerifyUserDto dto)
        {
            return new VerifyUser
            {             
                User = new User
                {
                    Id = dto.UserId
                },
                IsUsed = dto.IsUsed,
                CreationDate = dto.CreationDate
            };
        }
    }
}
