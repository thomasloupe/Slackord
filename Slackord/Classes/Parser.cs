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
        public static List<Message> Messages { get; private set; } = new();
        public static int TotalMessageCount { get; private set; }


        public async Task ParseJsonFiles(IEnumerable<string> files, string channelName, Dictionary<string, List<Message>> channels)
        {
            Messages.Clear();
            TotalMessageCount = 0;
            const char character = '⬓';
            string parsingLine = $"Begin parsing JSON data for {channelName}...";
            int lineLength = parsingLine.Length / 2 + 1;
            int boxCharacters = lineLength + 6;

            string boxOutput = new(character, boxCharacters);
            string leadingSpaces = new(' ', (boxCharacters - lineLength - 2) / 2);

            MainPage.WriteToDebugWindow($"{boxOutput}\n⬓  {leadingSpaces}{parsingLine}{leadingSpaces}  ⬓\n{boxOutput}\n");

            List<Message> parsedMessages = new();
            string currentFile = string.Empty;
            foreach (string file in files)
            {
                currentFile = Path.GetFileNameWithoutExtension(file);
                var json = await File.ReadAllTextAsync(file);
                var parsed = JArray.Parse(json);
                foreach (JObject pair in parsed.Cast<JObject>())
                {
                    var rawTimeDate = pair["ts"];
                    double oldDateTime = (double)rawTimeDate;
                    string convertDateTime = Helpers.ConvertFromUnixTimestampToHumanReadableTime(oldDateTime).ToString("g");

                    string newDateTime = convertDateTime;

                    var message = new Message
                    {
                        Text = pair["text"]?.ToString(),
                        Timestamp = newDateTime
                    };

                    if (pair.ContainsKey("reply_count") && pair.ContainsKey("thread_ts"))
                    {
                        message.IsThreadStart = true;
                    }
                    else if (pair.ContainsKey("thread_ts") && !pair.ContainsKey("reply_count"))
                    {
                        message.IsThreadMessage = true;
                    }

                    Messages.Add(message);
                    parsedMessages.Add(message);
                    TotalMessageCount += 1;
                }
            }
            channels[channelName] = parsedMessages;

            parsingLine = $"Parsing for [{channelName}]>[{currentFile}] complete!";
            lineLength = parsingLine.Length / 2 + 1;
            boxCharacters = lineLength + 6;

            boxOutput = new(character, boxCharacters);
            leadingSpaces = new(' ', (boxCharacters - lineLength - 2) / 2);
            MainPage.WriteToDebugWindow("\n" + $"{boxOutput}\n{character}  {leadingSpaces}{parsingLine}{leadingSpaces}  {character}\n{boxOutput}\n\n");
        }
    }
}
