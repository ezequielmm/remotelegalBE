using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;
using System;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class CaseMapper : IMapper<Case, CaseDto, CreateCaseDto>
    {
        public CaseDto ToDto(Case model)
        {
            return new CaseDto
            {
                Id = model.Id,
                CreationDate = model.CreationDate,
                Name = model.Name,
                CaseNumber = model.CaseNumber,
                AddedById = model.AddedById,
            };
        }

        public Case ToModel(CaseDto dto)
        {
            return new Case
            {
                Id = dto.Id,
                CreationDate = dto.CreationDate.UtcDateTime,
                Name = dto.Name,
                CaseNumber = dto.CaseNumber,
            };
        }

        public Case ToModel(CreateCaseDto dto)
        {
            return new Case
            {
                CreationDate = DateTime.UtcNow,
                Name = dto.Name,
                CaseNumber = dto.CaseNumber
            };
        }
    }
}
