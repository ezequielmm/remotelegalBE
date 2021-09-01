using System.ComponentModel;

namespace PrecisionReporters.Platform.Shared.Enums
{
    public enum LogCategory
    {
        [Description("JOIN DEPOSITION NOTIFICATION")]
        JoinNotification,
        [Description("CANCEL DEPOSITION NOTIFICATION")]
        CancelNotification,
        [Description("RESCHEDULE DEPOSITION NOTIFICATION")]
        RescheduleNotification,
        [Description("REMINDER DEPOSITION NOTIFICATION")]
        ReminderNotification,
        [Description("EMAIL ACCOUNT VERIFICATION EMAIL")]
        AccountVerification,
        [Description("TWILIO NEW ROOM GENERATED")]
        TwilioNewRoom,
        [Description("TWILIO END ROOM EXECUTED")]
        TwilioEndRoom
    }
}
