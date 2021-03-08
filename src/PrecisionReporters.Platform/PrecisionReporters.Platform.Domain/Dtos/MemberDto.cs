using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class MemberDto
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public Guid UserId { get; set; }
        public Guid CaseId { get; set; }
    }
}
