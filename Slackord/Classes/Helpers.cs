namespace Slackord.Classes
{
    static class Helpers
    {
        public static string DeDupeURLs(string input)
        {
            input = input.Replace("<", "").Replace(">", "");

            string[] parts = input.Split('|');

            if (parts.Length == 2)
            {
                if (Uri.TryCreate(parts[0], UriKind.Absolute, out Uri uri1) &&
                    Uri.TryCreate(parts[1], UriKind.Absolute, out Uri uri2))
                {
                    if (uri1.GetLeftPart(UriPartial.Path) == uri2.GetLeftPart(UriPartial.Path))
                    {
                        input = input.Replace(parts[1] + "|", "");
                    }
                }
            }

            string[] parts2 = input.Split('|').Distinct().ToArray();
            input = string.Join("|", parts2);

            // Replacing any instance of %7C with a space.
            input = input.Replace("%7C", " ");

            // Replacing any instance of | with a space, just in case it was missed in the URL encoding.
            input = input.Replace("|", " ");

            return input;
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
