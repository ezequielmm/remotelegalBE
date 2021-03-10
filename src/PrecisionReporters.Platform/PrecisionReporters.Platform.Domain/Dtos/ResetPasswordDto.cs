using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class ResetPasswordDto
    {
        public Guid VerificationHash { get; set; }
        public string Password { get; set; }
    }
}
