using System.Collections.Generic;

namespace SoundWords.Tools
{
    public static class StringExtensions
    {
        public static string ToSeparatedString(this IEnumerable<string> values, char separator)
        {
            return string.Join(separator, values);
        }

        public static string ToSeparatedString(this IEnumerable<string> values, string separator)
        {
            return string.Join(separator, values);
        }
    }
}
