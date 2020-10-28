using System;

namespace PrecisionReporters.Platform.Api.Dtos
{
    public class CreateVerifyUserDto
    {
        public Guid UserId { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
