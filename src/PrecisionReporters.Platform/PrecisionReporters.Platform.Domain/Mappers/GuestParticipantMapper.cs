using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;
using System;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class GuestParticipantMapper : IMapper<Participant, AddParticipantDto, CreateGuestDto>
    {
        public AddParticipantDto ToDto(Participant model)
        {
            throw new NotImplementedException();
        }

        public Participant ToModel(AddParticipantDto dto)
        {
            return new Participant
            {
                Email = dto.EmailAddress.ToLower(),
                Role = dto.ParticipantType
            };
        }

        public Participant ToModel(CreateGuestDto dto)
        {
            return new Participant
            {
                Name = dto.Name,
                Email = dto.EmailAddress.ToLower(),
                Role = dto.ParticipantType,
                User = new User
                {
                    FirstName = dto.Name,
                    LastName = "",
                    EmailAddress = dto.EmailAddress.ToLower(),
                    IsGuest = true,
                    CompanyAddress = "Guest",
                    CompanyName = "Guest",
                    PhoneNumber = "Guest"
                }
            };
        }
    }
}
