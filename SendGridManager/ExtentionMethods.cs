using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SendGridManager
{
    public static class ExtentionMethods
    {
        public static string NormalizeFolderName(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return s;
            }

            return Regex.Replace(s, "[^\\w]", "-").Replace("---", "-").Trim();
        }

        public static string ToCsvLine(this IEnumerable<string> cols)
        {
            return string.Join(",", cols);
        }

        public static bool IsValidFileName(this string s)
        {
            return !string.IsNullOrWhiteSpace(s) && s.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) == -1;
        }

        public static string CorrectFileName(this string s, char replacement = '-')
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (var c in s)
            {
                if (invalidChars.Contains(c))
                {
                    s = s.Replace(c, replacement);
                }
            }

            return s;
        }
    }
}
