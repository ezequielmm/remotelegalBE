using System;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class GuestParticipantMapper : IMapper<Participant, object, CreateGuestDto>
    {
        public object ToDto(Participant model)
        {
            throw new NotImplementedException();
        }

        public Participant ToModel(object dto)
        {
            throw new NotImplementedException();
        }

        public Participant ToModel(CreateGuestDto dto)
        {
            return new Participant
            {
                Name = dto.Name,
                Email = dto.EmailAddress,
                Role = dto.ParticipantType,
                User = new User
                {
                    FirstName = dto.Name,
                    LastName = "",
                    EmailAddress = dto.EmailAddress,
                    IsGuest = true,
                    CompanyAddress = "Guest",
                    CompanyName = "Guest",
                    PhoneNumber = "Guest"
                }
            };
        }
    }
}
