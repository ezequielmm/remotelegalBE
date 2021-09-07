using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace PrecisionReporters.Platform.Domain.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum enumVal)
        {
            var type = enumVal.GetType();
            var memInfo = type.GetMember(enumVal.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            return (attributes.Length > 0) ? ((DescriptionAttribute)attributes[0]).Description : enumVal.ToString();
        }

        public static T ParseDescriptionToEnum<T>(this string enumDescription)
            where T : Enum
        {
            try
            {
                return Enum.GetValues(typeof(T)).Cast<T>().FirstOrDefault(x => x.GetDescription() == enumDescription);
            }
            catch (Exception ex)
            {
                throw new Exception($"Invalid Description {enumDescription}", ex);
            }   
        }
    }
}
