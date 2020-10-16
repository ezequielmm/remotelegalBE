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
                CreatedDate = model.CreatedDate,
                Name = model.Name
            };
        }

        public Case ToModel(CaseDto dto)
        {
            return new Case
            {
                Id = dto.Id,
                CreatedDate = dto.CreatedDate,
                Name = dto.Name
            };
        }

        public Case ToModel(CreateCaseDto dto)
        {
            return new Case
            {
                Id = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                Name = dto.Name
            };
        }
    }
}
