﻿using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;

namespace PrecisionReporters.Platform.Domain.Mappers
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
                DepositionId = model.Deposition.Id,
                StampLabel = model.StampLabel
            };
        }

        public DepositionDocument ToModel(DepositionDocumentDto dto)
        {
            return new DepositionDocument
            {
                Id = dto.Id,
                CreationDate = dto.CreationDate.UtcDateTime,
                DocumentId = dto.DocumentId,
                DepositionId = dto.DepositionId,
                StampLabel = dto.StampLabel
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
