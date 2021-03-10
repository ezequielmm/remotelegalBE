using System;
using System.Collections.Generic;
using System.Text;

namespace PrecisionReporters.Platform.Domain.Dtos
{
    public class VerifyForgotPasswordDto
    {
        public Guid VerificationHash { get; set; }
    }
}
