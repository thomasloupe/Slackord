using MenuApp;
using Newtonsoft.Json.Linq;

namespace Slackord.Classes
{
    class Parser
    {
        public static readonly List<bool> isThreadMessages = new();
        public static readonly List<bool> isThreadStart = new();
        public static int TotalMessageCount;

        public async Task ParseJsonFiles(IEnumerable<string> files, string channelName, Dictionary<string, List<string>> channels)
        {
            const char character = '⬓';
            string parsingLine = $"Begin parsing JSON data for {channelName}...";
            int lineLength = parsingLine.Length / 2 + 1;
            int boxCharacters = lineLength + 6;

            string boxOutput = new(character, boxCharacters);
            string leadingSpaces = new(' ', (boxCharacters - lineLength - 2) / 2);

            MainPage.WriteToDebugWindow($"{boxOutput}\n⬓  {leadingSpaces}{parsingLine}{leadingSpaces}  ⬓\n{boxOutput}\n");

            try
            {
                List<string> parsedMessages = new();

                string currentMessageParsing;
                string currentFile = "";
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
                        ParseThreads(pair);

                        if (pair.ContainsKey("text") && !pair.ContainsKey("bot_profile"))
                        {
                            string slackUserName = "";
                            string slackRealName = "";

                            if (pair.ContainsKey("user_profile"))
                            {
                                slackUserName = pair["user_profile"]["display_name"].ToString();
                                slackRealName = pair["user_profile"]["real_name"].ToString();
                            }

                            string slackMessage = pair["text"].ToString();

                            slackMessage = Helpers.DeDupeURLs(slackMessage);

                            if (string.IsNullOrEmpty(slackUserName))
                            {
                                if (string.IsNullOrEmpty(slackRealName))
                                {
                                    currentMessageParsing = newDateTime + " - " + slackMessage;
                                }
                                else
                                {
                                    currentMessageParsing = newDateTime + " - " + slackRealName + ": " + slackMessage;
                                }
                                parsedMessages.Add(currentMessageParsing);
                                TotalMessageCount += 1;
                            }
                            else
                            {
                                currentMessageParsing = newDateTime + " - " + slackUserName + ": " + slackMessage;
                                if (currentMessageParsing.Length >= 2000)
                                {
                                    MainPage.WriteToDebugWindow($@"
                                    The following parse is over 2000 characters. Discord does not allow messages over 2000 characters.
                                    This message will be split into multiple posts. The message that will be split is: {currentMessageParsing}
                                    ");

                                    foreach (var part in Helpers.SplitInParts(currentMessageParsing, 1995))
                                    {
                                        parsedMessages.Add(part);
                                        TotalMessageCount += 1;
                                    }
                                }
                                else
                                {
                                    parsedMessages.Add(currentMessageParsing);
                                    TotalMessageCount += 1;
                                }
                            }

                        }

                        if (pair.ContainsKey("files") && pair["files"] is JArray filesArray && filesArray.Count > 0)
                        {
                            var fileLink = filesArray[0]["url_private"]?.ToString();

                            if (!string.IsNullOrEmpty(fileLink))
                            {
                                currentMessageParsing = fileLink;
                            }
                        }

                        if (pair.ContainsKey("bot_profile"))
                        {
                            string messagePart = null;
                            try
                            {
                                messagePart = pair["bot_profile"]?["name"]?.ToString() + ": " + pair["text"] + "\n";
                                currentMessageParsing = messagePart;
                                parsedMessages.Add(currentMessageParsing);
                                TotalMessageCount += 1;
                            }
                            catch (Exception ex)
                            {
                                MainPage.WriteToDebugWindow(ex.ToString() + "\n" + "Looking for bot_id instead...");
                                try
                                {
                                    messagePart = pair["bot_id"].ToString() + ": " + pair["text"] + "\n";
                                    currentMessageParsing = messagePart;
                                    parsedMessages.Add(currentMessageParsing);
                                    TotalMessageCount += 1;
                                }
                                catch (Exception innerex)
                                {
                                    MainPage.WriteToDebugWindow(innerex.ToString() + "\n\n" + "Message: " + messagePart + "\n\n" + "File: " + currentFile);
                                    currentMessageParsing = "The bot message was ignored.\n" +
                                        "Please submit an issue on Github for this. The message was: \n\n" + messagePart;
                                }
                            }
                        }

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
            catch (Exception ex)
            {
                MainPage.WriteToDebugWindow($"\n\n{ex.Message}\n\n");
            }
        }

        private static void ParseThreads(JObject pair)
        {
            if (pair.ContainsKey("reply_count") && pair.ContainsKey("thread_ts"))
            {
                isThreadStart.Add(true);
                isThreadMessages.Add(false);
            }
            else if (pair.ContainsKey("thread_ts") && !pair.ContainsKey("reply_count"))
            {
                isThreadStart.Add(false);
                isThreadMessages.Add(true);
            }
            else
            {
                isThreadStart.Add(false);
                isThreadMessages.Add(false);
            }
        }
    }
}