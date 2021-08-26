using System.Collections.Generic;
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
    }
}
