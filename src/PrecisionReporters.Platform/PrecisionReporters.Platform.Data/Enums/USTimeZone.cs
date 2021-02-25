using System.ComponentModel;

namespace PrecisionReporters.Platform.Data.Enums
{
    //TODO: remove this enum and add IANA list in DB.
    public enum USTimeZone
    {
        [Description("Eastern Standard Time")]
        EST,
        [Description("Central Standard Time")]
        CST,
        [Description("Mountain Standard Time")]
        MST,
        [Description("Pacific Standard Time")]
        PST,
        [Description("Alaska Standard Time")]
        AKST,
        [Description("Hawaii Standard Time")]
        HST
    }
}
