using System;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class CreateMemberDto
    {
        public Guid UserId { get; set; }
        public Guid CaseId { get; set; }
    }
}
