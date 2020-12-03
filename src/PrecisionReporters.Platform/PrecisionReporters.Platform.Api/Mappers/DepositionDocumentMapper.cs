using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class DepositionDocumentMapper : IMapper<DepositionDocument, DepositionDocumentDto, CreateDepositionDocumentDto>
    {
        public DepositionDocument ToModel(DepositionDocumentDto dto)
        {
            return new DepositionDocument
            {
                Id = dto.Id,
                CreationDate = dto.CreationDate.UtcDateTime,
                Name = dto.Name,
                AddedById = dto.AddedBy.Id
            };
        }

        public DepositionDocument ToModel(CreateDepositionDocumentDto dto)
        {
            return new DepositionDocument
            {
                Name = dto.Name
            };
        }

        public DepositionDocumentDto ToDto(DepositionDocument model)
        {
            return new DepositionDocumentDto
            {
                Id = model.Id,
                CreationDate = model.CreationDate,
                Name = model.Name,
                AddedBy = new UserOutputDto
                {
                    Id = model.AddedBy.Id,
                    FirstName = model.AddedBy.FirstName,
                    LastName = model.AddedBy.LastName
                },
            };
        }
    }
}
