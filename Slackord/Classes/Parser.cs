using MenuApp;
using Newtonsoft.Json.Linq;

namespace Slackord.Classes
{
    public class Message
    {
        public string Text { get; set; }
        public string Timestamp { get; set; }
        public bool IsThreadStart { get; set; }
        public bool IsThreadMessage { get; set; }
    }

    public class Parser
    {
        public static Dictionary<string, List<Message>> ThreadedMessages { get; private set; } = new Dictionary<string, List<Message>>();
        public static List<Message> OrphanedMessages { get; private set; } = new List<Message>();
        public static int TotalMessageCount { get; private set; }

        public async Task ParseJsonFiles(IEnumerable<string> files, string channelName, Dictionary<string, List<Message>> channels)
        {
            ThreadedMessages.Clear();
            OrphanedMessages.Clear();
            TotalMessageCount = 0;
            DisplayDebugBox($"Begin parsing JSON data for {channelName}...");

            List<Message> parsedMessages = new();

            foreach (string file in files)
            {
                await ParseFile(file);
            }

            foreach (var orphanedMessage in OrphanedMessages.ToList())
            {
                string threadTimestamp = orphanedMessage.Timestamp;
                if (ThreadedMessages.ContainsKey(threadTimestamp))
                {
                    ThreadedMessages[threadTimestamp].Add(orphanedMessage);
                    OrphanedMessages.Remove(orphanedMessage);
                }
            }

            parsedMessages.AddRange(ThreadedMessages.Values.SelectMany(x => x).OrderBy(m => m.Timestamp));
            parsedMessages.AddRange(OrphanedMessages.OrderBy(m => m.Timestamp));

            channels[channelName] = parsedMessages;
            DisplayDebugBox($"Parsing for [{channelName}] is complete!");
        }

        private static async Task ParseFile(string file)
        {
            var json = await File.ReadAllTextAsync(file);
            var parsed = JArray.Parse(json);

            foreach (JObject pair in parsed.Cast<JObject>())
            {
                ParseMessage(pair);
                TotalMessageCount += 1;
            }
        }

        private static void ParseMessage(JObject pair)
        {
            var rawTimeDate = pair["ts"];
            double oldDateTime = (double)rawTimeDate;
            string convertDateTime = Helpers.ConvertFromUnixTimestampToHumanReadableTime(oldDateTime).ToString("g");

            var message = new Message
            {
                Text = Helpers.DeDupeURLs(pair["text"]?.ToString() ?? ""),
                Timestamp = convertDateTime
            };

            string threadTimestamp = pair["thread_ts"]?.ToString();

            // Identify Thread Starters (messages where "thread_ts" matches their own "ts" and have both "reply_count" and "replies" fields)
            if (threadTimestamp == rawTimeDate.ToString() && pair.ContainsKey("reply_count") && pair.ContainsKey("replies"))
            {
                message.IsThreadStart = true;
                if (!ThreadedMessages.ContainsKey(threadTimestamp))
                {
                    ThreadedMessages[threadTimestamp] = new List<Message> { message };
                }
            }
            // Identify Thread Replies (messages where "thread_ts" is different from their "ts")
            else if (threadTimestamp != null && threadTimestamp != rawTimeDate.ToString())
            {
                message.IsThreadMessage = true;
                if (ThreadedMessages.ContainsKey(threadTimestamp))
                {
                    ThreadedMessages[threadTimestamp].Add(message);
                }
                else
                {
                    OrphanedMessages.Add(message);
                }
            }
            // Handle Standalone Messages (messages without the above conditions)
            else
            {
                OrphanedMessages.Add(message);
            }
        }

        private static void DisplayDebugBox(string message)
        {
            const char character = '⬓';
            int lineLength = message.Length / 2 + 1;
            int boxCharacters = lineLength + 6;

            string boxOutput = new(character, boxCharacters);
            string leadingSpaces = new(' ', (boxCharacters - lineLength - 2) / 2);

            MainPage.WriteToDebugWindow($"{boxOutput}\n⬓  {leadingSpaces}{message}{leadingSpaces}  ⬓\n{boxOutput}\n");
        }
    }
}
