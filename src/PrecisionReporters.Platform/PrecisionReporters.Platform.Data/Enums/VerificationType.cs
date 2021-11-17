using System.ComponentModel;

namespace PrecisionReporters.Platform.Data.Enums
{
    public enum VerificationType
    {
        [Description("VerificationEmailTemplate")]
        VerifyUser,
        [Description("ForgotPasswordEmailTemplate")]
        ForgotPassword
    }
}
