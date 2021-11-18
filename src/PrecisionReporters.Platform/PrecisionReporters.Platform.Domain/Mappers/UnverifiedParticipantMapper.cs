using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class UnverifiedUserMapper: IMapper<Participant, SignInUnverifiedUserDto, object>
    {
        public Participant ToModel(SignInUnverifiedUserDto dto)
        {
            return new Participant
            {
                Email = dto.EmailAddress.ToLower(),
                Role = dto.ParticipantType,
                User = new User
                {
                    EmailAddress = dto.EmailAddress.ToLower(),
                    Password = dto.Password
                }
            };
        }

        public Participant ToModel(object dto)
        {
            throw new System.NotImplementedException();
        }

        public SignInUnverifiedUserDto ToDto(Participant model)
        {
            throw new System.NotImplementedException();
        }
    }
}