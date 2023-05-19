using Newtonsoft.Json.Linq;

namespace Slackord
{
    class Parser
    {
        private readonly Editor debugWindow;
        public JArray parsed;
        public bool _isFileParsed;
        public static readonly List<bool> isThreadMessages = new();
        public static readonly List<bool> isThreadStart = new();
        public static int TotalMessageCount;

        public Parser(Editor debugWindow)
        {
            this.debugWindow = debugWindow;
        }

        public async Task ParseJsonFiles(List<string> files, string channelName, Dictionary<string, List<string>> channels)
        {
            debugWindow.Text += ($"""
            Begin parsing JSON data for {channelName}...
            -----------------------------------------

            """);
            try
            {
                List<string> parsedMessages = new();

                string currentMessageParsing;
                string currentFile = "";
                foreach (string file in files)
                {
                    currentFile = Path.GetFileNameWithoutExtension(file);
                    var json = File.ReadAllText(file);
                    parsed = JArray.Parse(json);
                    foreach (JObject pair in parsed.Cast<JObject>())
                    {
                        var rawTimeDate = pair["ts"];
                        double oldDateTime = (double)rawTimeDate;
                        string convertDateTime = Helpers.ConvertFromUnixTimestampToHumanReadableTime(oldDateTime).ToString("g");
                        string newDateTime = convertDateTime.ToString();

                        // JSON message thread handling.
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

                        // JSON message parsing.
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
                                    parsedMessages.Add(currentMessageParsing);
                                    TotalMessageCount += 1;
                                }
                                else
                                {
                                    currentMessageParsing = newDateTime + " - " + slackRealName + ": " + slackMessage;
                                    parsedMessages.Add(currentMessageParsing);
                                    TotalMessageCount += 1;
                                }
                            }
                            else
                            {
                                currentMessageParsing = newDateTime + " - " + slackUserName + ": " + slackMessage;
                                if (currentMessageParsing.Length >= 2000)
                                {
                                    debugWindow.Text += ($@"
                                    The following parse is over 2000 characters. Discord does not allow messages over 2000 characters.
                                    This message will be split into multiple posts. The message that will be split is: {currentMessageParsing}
                                    ");
                                }
                                else
                                {
                                    currentMessageParsing = newDateTime + " - " + slackUserName + ": " + slackMessage;
                                    parsedMessages.Add(currentMessageParsing);
                                    TotalMessageCount += 1;
                                }
                            }
                            debugWindow.Text += (currentMessageParsing + "\n");
                        }

                        if (pair.ContainsKey("files") && pair["files"] is JArray filesArray && filesArray.Count > 0)
                        {
                            var fileLink = filesArray[0]["url_private"]?.ToString();

                            if (!string.IsNullOrEmpty(fileLink))
                            {
                                currentMessageParsing = fileLink;
                                debugWindow.Text += (currentMessageParsing + "\n");
                            }
                        }

                        if (pair.ContainsKey("bot_profile"))
                        {
                            try
                            {
                                currentMessageParsing = pair["bot_profile"]["name"].ToString() + ": " + pair["text"] + "\n";
                                parsedMessages.Add(currentMessageParsing);
                                TotalMessageCount += 1;
                            }
                            catch (NullReferenceException)
                            {
                                try
                                {
                                    currentMessageParsing = pair["bot_id"].ToString() + ": " + pair["text"] + "\n";
                                    parsedMessages.Add(currentMessageParsing);
                                    TotalMessageCount += 1;
                                }
                                catch (NullReferenceException)
                                {
                                    currentMessageParsing = "A bot message was ignored. Please submit an issue on Github for this.";
                                }
                            }
                            debugWindow.Text += (currentMessageParsing + "\n");
                        }
                    }
                }
                channels[channelName] = parsedMessages;
                debugWindow.Text += ($"""
                -----------------------------------------
                Parsing of {currentFile} completed successfully!
                -----------------------------------------

                """);

                _isFileParsed = true;
                debugWindow.TextColor = new Color(255, 255, 255, 255);
            }
            catch (Exception ex)
            {
                Page page = new();
                await page.DisplayAlert("Error", ex.Message, "OK");
            }
            await Task.CompletedTask;
        }
    }
}
