using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Slackord.Classes
{
    static class Helpers
    {
        public static string DeDupeURLs(string input)
        {
            // First issue: Replace %7C with /
            var regex1 = new Regex(@"<(https?://[^\|>%7C]+)%7C([^>]+)>");
            input = regex1.Replace(input, "<$1/$2>");

            // Second issue: Deduplicate URLs
            var regex2 = new Regex(@"<((https?://|www\.)[^\|>%7C]+)\|((https?://|www\.)[^\|>%7C]+)>");
            return regex2.Replace(input, match =>
            {
                // If the two URLs are the same
                if (match.Groups[1].Value == match.Groups[3].Value)
                    return $"<{match.Groups[1].Value}>";

                // If the base of the two URLs is the same
                if (match.Groups[1].Value == match.Groups[3].Value.Substring(0, match.Groups[1].Value.Length))
                    return $"<{match.Groups[3].Value}>";

                // If neither of the above conditions are met, return the original matched string
                return match.Value;
            });
        }

        public static DateTime ConvertFromUnixTimestampToHumanReadableTime(double timestamp)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var returnDate = date.AddSeconds(timestamp);
            return returnDate;
        }

        public static IEnumerable<string> SplitInParts(this string s, int partLength)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (partLength <= 0)
                throw new ArgumentException("Invalid char length specified.", nameof(partLength));

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }
    }
}
