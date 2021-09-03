using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Shared.Dtos;
using System;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class ExhibitDocumentMapper : IMapper<Document, DocumentDto, object>
    {
        public Document ToModel(DocumentDto dto)
        {
            return new Document
            {
                CreationDate = dto.CreationDate == null ? DateTime.Now.ToUniversalTime() : dto.CreationDate.Value.ToUniversalTime(),
                DisplayName = dto.DisplayName,
                Size = dto.Size,
                Name = dto.Name,
                AddedById = dto.AddedBy,
                SharedAt = dto.SharedAt?.ToUniversalTime(),
                Type = dto.Type,
                FilePath = dto.FilePath,
            };
        }

        public Document ToModel(object dto)
        {
            throw new System.NotImplementedException();
        }

        public DocumentDto ToDto(Document model)
        {
            throw new System.NotImplementedException();
        }
    }
}
