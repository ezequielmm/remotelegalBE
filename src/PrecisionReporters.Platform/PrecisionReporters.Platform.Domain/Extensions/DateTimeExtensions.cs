using System;
using System.Linq;
using TimeZoneConverter;
namespace PrecisionReporters.Platform.Domain.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ConvertTimeZone(this DateTime dateTime, string timeZone)
        {
            var timeZoneAbbreviation = Enum.GetValues(typeof(Enums.USTimeZone)).Cast<Enums.USTimeZone>().FirstOrDefault(x => x.GetDescription() == timeZone).ToString();
            var timeZoneInfo = TZConvert.GetTimeZoneInfo(timeZone);
            DateTime convertedTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeZoneInfo);

            return $"{convertedTime:hh:mm tt} {timeZoneAbbreviation}";
        }

        public static string ConvertTime(this DateTime dateTime, string timeZone)
        {
            var timeZoneInfo = TZConvert.GetTimeZoneInfo(timeZone);
            DateTime convertedTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeZoneInfo);

            return convertedTime.ToShortTimeString();
        }

        public static DateTime GetConvertedTime(this DateTime dateTime, string timeZone)
        {
            var timeZoneInfo = TZConvert.GetTimeZoneInfo(timeZone);
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, TimeZoneInfo.FindSystemTimeZoneById(timeZoneInfo.Id));
        }

        public static string GetFormattedDateTime(this DateTime dateTime, string timeZone)
        {
            var timeZoneAbbreviation = Enum.GetValues(typeof(Enums.USTimeZone)).Cast<Enums.USTimeZone>().FirstOrDefault(x => x.GetDescription() == timeZone).ToString();
            var timeZoneInfo = TZConvert.GetTimeZoneInfo(timeZone);
            DateTime convertedTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeZoneInfo);

            return $"{convertedTime:MMM d, yyyy hh:mm tt} {timeZoneAbbreviation}";
        }

        public static DateTime GetWithSpecificTimeZone(this DateTime dateTime, string timeZone)
        {
            var timeZoneInfo = TZConvert.GetTimeZoneInfo(timeZone);
            return TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.FindSystemTimeZoneById(timeZoneInfo.Id));
        }
    }
}