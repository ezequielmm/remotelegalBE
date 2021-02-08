using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;
using System;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class DocumentMapper : IMapper<Document, DocumentDto, CreateDocumentDto>
    {
        public Document ToModel(DocumentDto dto)
        {
            return new Document
            {
                Id = dto.Id,
                CreationDate = dto.CreationDate.UtcDateTime,
                DisplayName = dto.DisplayName,
                Size = dto.Size,
                Name = dto.Name,
                AddedById = dto.AddedBy.Id,
                SharedAt = dto.SharedAt?.UtcDateTime
            };
        }

        public Document ToModel(CreateDocumentDto dto)
        {
            return new Document
            {
                Name = dto.Name
            };
        }

        public DocumentDto ToDto(Document model)
        {
            return new DocumentDto
            {
                Id = model.Id,
                CreationDate = model.CreationDate,
                DisplayName = model.DisplayName,
                Size = model.Size,
                Name = model.Name,
                AddedBy = new UserOutputDto
                {
                    Id = model.AddedBy.Id,
                    FirstName = model.AddedBy.FirstName,
                    LastName = model.AddedBy.LastName
                },
                SharedAt = model.SharedAt.HasValue ? new DateTimeOffset(model.SharedAt.Value, TimeSpan.Zero) : (DateTimeOffset?)null
            };
        }
    }
}
