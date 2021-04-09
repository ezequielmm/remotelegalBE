using System;
using System.Linq;
using TimeZoneConverter;

namespace PrecisionReporters.Platform.Domain.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ConvertTimeZone(this DateTime time, string timeZone)
        {
            var timeZoneAbbreviation = Enum.GetValues(typeof(Enums.USTimeZone)).Cast<Enums.USTimeZone>().FirstOrDefault(x => x.GetDescription() == timeZone).ToString();
            var timeZoneInfo = TZConvert.GetTimeZoneInfo(timeZone);
            DateTime convertedTime = TimeZoneInfo.ConvertTimeFromUtc(time, timeZoneInfo);

            return $"{convertedTime.ToShortTimeString()} {timeZoneAbbreviation}";
        }

        public static string ConvertTime(this DateTime time, string timeZone)
        {
            var timeZoneAbbreviation = Enum.GetValues(typeof(Enums.USTimeZone)).Cast<Enums.USTimeZone>().FirstOrDefault(x => x.GetDescription() == timeZone).ToString();
            var timeZoneInfo = TZConvert.GetTimeZoneInfo(timeZone);
            DateTime convertedTime = TimeZoneInfo.ConvertTimeFromUtc(time, timeZoneInfo);

            return convertedTime.ToShortTimeString();
        }
    }
}
