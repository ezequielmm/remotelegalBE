using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Domain.Dtos;

namespace PrecisionReporters.Platform.Domain.Mappers
{
    public class EditCaseMapper: IMapper<Case, EditCaseDto, object>
    {
        public Case ToModel(EditCaseDto dto)
        {
            return new Case
            {
                Name = dto.Name,
                CaseNumber = dto.CaseNumber
            };
        }

        public Case ToModel(object dto)
        {
            throw new System.NotImplementedException();
        }

        public EditCaseDto ToDto(Case model)
        {
            throw new System.NotImplementedException();
        }
    }
}