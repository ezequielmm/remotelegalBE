using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class DepositionDocumentMapper : IMapper<DepositionDocument, DepositionDocumentDto, CreateDepositionDocumentDto>
    {
        public DepositionDocumentDto ToDto(DepositionDocument model)
        {
            return new DepositionDocumentDto
            {
                Id = model.Id,
                CreationDate = model.CreationDate,
                DocumentId = model.Document.Id,
                DepositionId = model.Deposition.Id
            };
        }

        public DepositionDocument ToModel(DepositionDocumentDto dto)
        {
            return new DepositionDocument
            {
                Id = dto.Id,
                CreationDate = dto.CreationDate.UtcDateTime,
                DocumentId = dto.DocumentId,
                DepositionId = dto.DepositionId
            };
        }

        public DepositionDocument ToModel(CreateDepositionDocumentDto dto)
        {
            return new DepositionDocument
            {
                DocumentId = dto.DocumentId,
                DepositionId = dto.DepositionId
            };
        }
    }
}
