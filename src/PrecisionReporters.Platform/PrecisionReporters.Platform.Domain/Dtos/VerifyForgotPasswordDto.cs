using System;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class VerifyForgotPasswordDto
    {
        public Guid VerificationHash { get; set; }
    }
}
