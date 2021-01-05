using System;
using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class ParticipantMapper : IMapper<Participant, ParticipantDto, CreateParticipantDto>
    {
        public Participant ToModel(ParticipantDto dto)
        {
            return new Participant
            {
                Id = dto.Id,
                CreationDate = dto.CreationDate.UtcDateTime,
                Email = dto.Email,
                Name = dto.Name,
                Phone = dto.Phone,
                Role = Enum.Parse<ParticipantType>(dto.Role, true),
                UserId = dto.User.Id
            };
        }

        public Participant ToModel(CreateParticipantDto dto)
        {
            return new Participant
            {
                Email = dto.Email,
                Name = dto.Name,
                Phone = dto.Phone,
                Role = dto.Role
            };
        }

        public ParticipantDto ToDto(Participant model)
        {
            return new ParticipantDto
            {
                Id = model.Id,
                CreationDate = model.CreationDate,
                Email = model.Email,
                Name = model.Name,
                Phone = model.Phone,
                Role = model.Role.ToString(),
                User = model.User != null ?
                    new UserOutputDto
                    {
                        Id = model.User.Id,
                        FirstName = model.User.FirstName,
                        LastName = model.User.LastName
                    }
                    : null
            };
        }
    }
}
