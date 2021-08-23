using System.Linq;
using System.Text.RegularExpressions;

namespace PrecisionReporters.Platform.Shared.Extensions
{
    public static class StringExtensions
    {
        public static string ToHypenCase(this string text)
        {
            var pattern =
                new Regex(@"[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*|[A-Z]|[0-9]+");

            return text == null
                ? null
                : string
                    .Join("-", pattern.Matches(text).Cast<Match>().Select(m => m.Value))
                    .ToLower();
        }
    }
}
