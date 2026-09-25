using System.Linq;
using System.Text;

namespace discogs
{
    public static class CsvExtensions
    {
        private static readonly char[] _escapedChars = { ',', '"', '\r', '\n' };
        public static string ToCsv(params string[] values)
            => string.Join(",", values.Select(SafeCsv));

        private static string SafeCsv(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }

            var quoted = s.Replace("\"", "\"\"").Trim();

            // this produces a cleaner CSV, however
            // it is profoundly disliked by Excel which translated values like "0003" into 3
            // and "58152000018389" into 5.8152E+13
            if (quoted.IndexOfAny(_escapedChars) != -1)
            {
                quoted = $"\"{quoted}\"";
            }

            return quoted;
        }

        // https://stackoverflow.com/a/6377656
        private static string SaveCsv2(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return "";
            }

            bool mustQuote = (str.Contains(",") || str.Contains("\"") || str.Contains("\r") || str.Contains("\n"));
            if (mustQuote)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("\"");
                foreach (char nextChar in str)
                {
                    sb.Append(nextChar);
                    if (nextChar == '"')
                        sb.Append("\"");
                }
                sb.Append("\"");
                return sb.ToString();
            }

            return str;
        }
    }
}