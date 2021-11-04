using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class EditParticipantMapper: IMapper<Participant, EditParticipantDto, object>
    {
        public Participant ToModel(EditParticipantDto dto)
        {
            return new Participant
            {
                Id = dto.Id,
                Email = dto.Email?.ToLower(),
                Name = dto.Name,
                LastName = dto.LastName,
                Phone = dto.Phone,
                Role = dto.Role
            };
        }

        public Participant ToModel(object dto)
        {
            throw new System.NotImplementedException();
        }

        public EditParticipantDto ToDto(Participant model)
        {
            throw new System.NotImplementedException();
        }
    }
}