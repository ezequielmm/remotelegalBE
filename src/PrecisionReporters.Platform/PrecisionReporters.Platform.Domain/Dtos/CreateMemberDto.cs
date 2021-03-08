using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class CreateMemberDto
    {
        public Guid UserId { get; set; }
        public Guid CaseId { get; set; }
    }
}
