using System.ComponentModel;

namespace PrecisionReporters.Platform.Domain.Enums
{
    public enum CalendarAction
    {
        [Description("REQUEST")]
        Add,
        [Description("CANCEL")]
        Cancel,
        [Description("REQUEST")]
        Update
    }
}
