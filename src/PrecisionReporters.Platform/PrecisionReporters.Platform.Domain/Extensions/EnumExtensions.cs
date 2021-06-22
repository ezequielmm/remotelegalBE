using System;
using System.ComponentModel;

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
    }
}
