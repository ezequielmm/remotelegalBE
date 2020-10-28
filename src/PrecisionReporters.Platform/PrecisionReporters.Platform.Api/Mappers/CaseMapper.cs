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
                Name = model.Name
            };
        }

        public Case ToModel(CaseDto dto)
        {
            return new Case
            {
                Id = dto.Id,
                CreationDate = dto.CreationDate,
                Name = dto.Name
            };
        }

        public Case ToModel(CreateCaseDto dto)
        {
            return new Case
            {
                //TODO: Remove NewGuid EF will take charge of that
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow,
                Name = dto.Name
            };
        }
    }
}
