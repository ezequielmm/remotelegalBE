using PrecisionReporters.Platform.Api.Dtos;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Api.Mappers
{
    public class MemberMapper : IMapper<Member, MemberDto, CreateMemberDto>
    {
        public Member ToModel(MemberDto dto)
        {
            return new Member
            {
                Id = dto.Id,
                CreationDate = dto.CreationDate.UtcDateTime,
                CaseId = dto.CaseId,
                UserId = dto.UserId
            };
        }

        public Member ToModel(CreateMemberDto dto)
        {
            return new Member
            {
                CaseId = dto.CaseId,
                UserId = dto.UserId
            };
        }

        public MemberDto ToDto(Member model)
        {
            return new MemberDto
            {
                Id = model.Id,
                CreationDate = model.CreationDate,
                CaseId = model.Case.Id,
                UserId = model.User.Id
            };
        }
    }
}
