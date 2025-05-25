using System.Text.RegularExpressions;
using Application = Microsoft.Maui.Controls.Application;

namespace Slackord.Classes
{
    public partial class Reconstruct
    {
        internal static readonly Dictionary<string, DeconstructedUser> UsersDict = [];
        private static readonly Dictionary<string, ThreadInfo> threadDictionary = [];
        public static IReadOnlyDictionary<string, ThreadInfo> ThreadDictionary => threadDictionary;

        public static void InitializeUsersDict(Dictionary<string, DeconstructedUser> usersDict)
        {
            foreach (KeyValuePair<string, DeconstructedUser> kvp in usersDict)
            {
                UsersDict[kvp.Key] = kvp.Value;
            }
        }

        [GeneratedRegex(@"\*\*((?:[^*]|(?:\*(?!\*)))*)\*\*")]
        private static partial Regex Bold();

        [GeneratedRegex(@"\*((?:[^*]|(?:\*(?!\*)))*)\*")]
        private static partial Regex Italics();

        [GeneratedRegex(@"__((?:[^_]|(?:_(?!_)))*)__")]
        private static partial Regex Underline();

        [GeneratedRegex(@"~~((?:[^~]|(?:~(?!~)))*)~~")]
        private static partial Regex Strikethrough();

        [GeneratedRegex(@"<(https?://[^|]+)\|(.*?)>")]
        private static partial Regex MaskedLinks();

        [GeneratedRegex(@"&gt; (.+?)(?=\n|$)")]
        private static partial Regex BlockQuotes();


        public static async Task ReconstructAsync(List<Channel> channels, CancellationToken cancellationToken)
        {
            try
            {
                ProcessingManager.Instance.SetState(ProcessingState.ReconstructingMessages);
                ApplicationWindow.ShowProgressBar();

                // Calculate total messages for accurate progress tracking
                int totalMessages = channels.Sum(c => c.DeconstructedMessagesList.Count);
                int processedMessages = 0;

                // Time-based progress tracking
                DateTime lastProgressUpdate = DateTime.Now;
                const int progressUpdateIntervalSeconds = 15;

                Application.Current.Dispatcher.Dispatch(() =>
                {
                    ApplicationWindow.WriteToDebugWindow($"Starting reconstruction of {totalMessages:N0} messages across {channels.Count} channels\n");
                });

                for (int channelIndex = 0; channelIndex < channels.Count; channelIndex++)
                {
                    Channel channel = channels[channelIndex];
                    var resumeData = ResumeData.LoadResumeData().FirstOrDefault(rd => rd.ChannelName == channel.Name);
                    int startMessageIndex = resumeData?.LastMessagePosition + 1 ?? 0;

                    // Skip messages that were already processed during resume
                    processedMessages += startMessageIndex;

                    Application.Current.Dispatcher.Dispatch(() =>
                    {
                        ApplicationWindow.WriteToDebugWindow($"Processing channel {channelIndex + 1}/{channels.Count}: {channel.Name} " +
                            $"(starting from message {startMessageIndex + 1}/{channel.DeconstructedMessagesList.Count})\n");
                    });

                    for (int i = startMessageIndex; i < channel.DeconstructedMessagesList.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        DeconstructedMessage deconstructedMessage = channel.DeconstructedMessagesList[i];
                        await ReconstructMessage(deconstructedMessage, channel);

                        processedMessages++;

                        // TIME-BASED UPDATES: Only update every 15 seconds or on completion
                        DateTime now = DateTime.Now;
                        bool shouldUpdate = (now - lastProgressUpdate).TotalSeconds >= progressUpdateIntervalSeconds ||
                                           i == channel.DeconstructedMessagesList.Count - 1;

                        if (shouldUpdate)
                        {
                            double progressPercent = (double)processedMessages / totalMessages * 100;
                            ApplicationWindow.UpdateProgressBar(processedMessages, totalMessages, "messages");

                            Application.Current.Dispatcher.Dispatch(() =>
                            {
                                ApplicationWindow.WriteToDebugWindow($"Progress: {processedMessages:N0}/{totalMessages:N0} messages ({progressPercent:F1}%) - Current: {channel.Name}\n");
                            });

                            lastProgressUpdate = now;
                        }

                        // Update resume data
                        if (resumeData != null)
                        {
                            resumeData.LastMessagePosition = i;
                            ResumeData.SaveResumeData(ResumeData.LoadResumeData());
                        }
                    }

                    Application.Current.Dispatcher.Dispatch(() =>
                    {
                        ApplicationWindow.WriteToDebugWindow($"✅ Completed channel: {channel.Name} - {channel.ReconstructedMessagesList.Count:N0} messages reconstructed\n");
                    });
                }

                // Final update - ensure we show 100% completion
                ApplicationWindow.UpdateProgressBar(totalMessages, totalMessages, "messages");
                ProcessingManager.Instance.SetState(ProcessingState.ReadyForDiscordImport);

                Application.Current.Dispatcher.Dispatch(() =>
                {
                    ApplicationWindow.WriteToDebugWindow($"\n🎊 RECONSTRUCTION COMPLETE! 🎊\n");
                    ApplicationWindow.WriteToDebugWindow($"📊 Summary:\n");
                    ApplicationWindow.WriteToDebugWindow($"   • Channels processed: {channels.Count:N0}\n");
                    ApplicationWindow.WriteToDebugWindow($"   • Total messages reconstructed: {processedMessages:N0}\n");
                    ApplicationWindow.WriteToDebugWindow($"   • Ready for Discord import!\n\n");
                    ApplicationWindow.WriteToDebugWindow($"🚀 Next step: Use the Discord slash command '/slackord' to begin posting messages.\n\n");
                });

                await Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                ProcessingManager.Instance.SetState(ProcessingState.Error);
                Application.Current.Dispatcher.Dispatch(() =>
                {
                    ApplicationWindow.WriteToDebugWindow($"❌ Reconstruction operation was cancelled.\n");
                });
            }
            catch (Exception ex)
            {
                ProcessingManager.Instance.SetState(ProcessingState.Error);
                Application.Current.Dispatcher.Dispatch(() =>
                {
                    ApplicationWindow.WriteToDebugWindow($"❌ ReconstructAsync Error: {ex.Message}\n\n");
                });
            }
        }

        public static async Task ReconstructMessage(DeconstructedMessage deconstructedMessage, Channel channel)
        {
            try
            {
                string messageContent = string.IsNullOrEmpty(deconstructedMessage.Text) ? "File hidden by Slack limit" : ConvertToDiscordMarkdown(ReplaceUserMentions(deconstructedMessage.Text));
                string timestampString = deconstructedMessage.Timestamp?.ToString();
                if (string.IsNullOrEmpty(timestampString))
                {
                    Logger.Log(timestampString == null ? "Missing timestamp for message." : "Invalid timestamp format.");
                    return;
                }

                string displayTimestamp = ConvertTimestampToLocalizedString(timestampString);
                string userName = ConvertUserToDisplayName(deconstructedMessage.User);
                string userAvatar = deconstructedMessage.User != null && UsersDict.TryGetValue(deconstructedMessage.User, out DeconstructedUser user) ? user.Profile.Avatar : null;
                string formattedMessage = FormatMessage(messageContent, displayTimestamp);

                SplitAndAddMessages(formattedMessage, deconstructedMessage, channel, userName, userAvatar);

                if (deconstructedMessage.FileURLs.Count > 0)
                {
                    for (int i = 0; i < deconstructedMessage.FileURLs.Count; i++)
                    {
                        string fileUrl = deconstructedMessage.FileURLs[i];
                        bool isDownloadable = deconstructedMessage.IsFileDownloadable[i];
                        if (isDownloadable)
                        {
                            var (localFilePath, permalink) = await DownloadFile(fileUrl, channel.Name, deconstructedMessage.OriginalTimestamp, isDownloadable);
                            if (!string.IsNullOrEmpty(localFilePath))
                            {
                                int lastMessageIndex = channel.ReconstructedMessagesList.Count - 1;
                                channel.ReconstructedMessagesList[lastMessageIndex].FileURLs.Add(localFilePath);
                            }
                            else
                            {
                                Logger.Log($"File download for channel {channel.Name} failed. Original Slack Message: {deconstructedMessage.OriginalSlackMessageJson}");
                            }
                        }
                        else
                        {
                            int lastMessageIndex = channel.ReconstructedMessagesList.Count - 1;
                            channel.ReconstructedMessagesList[lastMessageIndex].Content += " [File hidden by Slack limit]";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"ReconstructMessage(): {ex.Message}");
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ReconstructMessage(): {ex.Message}\n"); });
            }
        }

        public static async Task<(string localFilePath, string permalink)> DownloadFile(string fileUrl, string channelName, string _originalTimestamp, bool isDownloadable)
        {
            if (!isDownloadable || string.IsNullOrWhiteSpace(fileUrl))
            {
                return (null, null);
            }

            // Normalize the file URL and check if it's a valid URI
            if (!Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute))
            {
                Logger.Log($"Invalid URL provided: {fileUrl}");
                return (null, null);
            }

            // Prepare directories and file path
            string downloadsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads", channelName);
            
            // Ensure the download directory exists
            Directory.CreateDirectory(downloadsFolder);
            string fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
            string sanitizedFileName = string.Concat(fileName.Split(Path.GetInvalidFileNameChars()));
            string localFilePath = Path.Combine(downloadsFolder, sanitizedFileName);

            // Download the file
            try
            {
                using HttpClient httpClient = new();
                HttpResponseMessage response = await httpClient.GetAsync(fileUrl);
                if (response.IsSuccessStatusCode)
                {
                    byte[] fileData = await response.Content.ReadAsByteArrayAsync();
                    if (File.Exists(localFilePath))
                    {
                        string fileExtension = Path.GetExtension(sanitizedFileName);
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sanitizedFileName);
                        localFilePath = Path.Combine(downloadsFolder, $"{fileNameWithoutExtension}_{Guid.NewGuid()}{fileExtension}");
                    }
                    await File.WriteAllBytesAsync(localFilePath, fileData);
                    return (localFilePath, fileUrl);
                }
                else
                {
                    Logger.Log($"Failed to download file from {fileUrl}. HTTP status: {response.StatusCode}");
                    return (null, null);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"DownloadFile(): Exception during file download: {ex.Message}");
                return (null, null);
            }
        }

        private static string ConvertTimestampToLocalizedString(string timestampString)
        {
            try
            {
                // Split the timestamp into whole and fractional seconds.
                string[] parts = timestampString.Split('.');
                if (parts.Length != 2)
                {
                    throw new ArgumentException("Invalid timestamp format");
                }

                long wholeSeconds = long.Parse(parts[0]);
                long fractionalTicks = long.Parse(parts[1]) * (TimeSpan.TicksPerSecond / 1_000_000); // Convert microseconds to ticks.

                // Create a DateTimeOffset from the Unix timestamp.
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(wholeSeconds).AddTicks(fractionalTicks);

                // Retrieve the current timestamp setting.
                string timestampValue = Preferences.Default.Get("TimestampValue", "12 Hour");

                // Determine the format string.
                string format = timestampValue == "24 Hour" ? "yyyy-MM-dd HH:mm:ss" : "yyyy-MM-dd hh:mm:ss tt";

                return dateTimeOffset.ToLocalTime().ToString(format);
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ConvertTimestampToLocalizedString() : {ex.Message}\n"); });
                return null;
            }
        }

        private static string ConvertToDiscordMarkdown(string input)
        {
            try
            {
                // Rich text conversions.
                input = Bold().Replace(input, "**$1**");       // Bold.
                input = Italics().Replace(input, "*$1*");      // Italics.
                input = Underline().Replace(input, "__$1__");  // Underline.
                input = Strikethrough().Replace(input, "~~$1~~"); // Strikethrough.
                input = MaskedLinks().Replace(input, "[$2]($1)");  // Masked Links.
                input = BlockQuotes().Replace(input, "> $1\n");    // Blockquotes.

                return input;
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ConvertToDiscordMarkdown() : {ex.Message}\n"); });
                return input;
            }
        }

        private static string ConvertUserToDisplayName(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || !UsersDict.TryGetValue(userId, out DeconstructedUser user))
                {
                    return "Unknown User";
                }

                string displayName = user.Profile.DisplayName;
                string realName = user.Profile.RealName;
                string userName = user.Name;

                return ApplicationWindow.CurrentUserFormatOrder switch
                {
                    ApplicationWindow.UserFormatOrder.DisplayName_User_RealName => !string.IsNullOrEmpty(displayName) ? displayName : (!string.IsNullOrEmpty(userName) ? userName : realName),
                    ApplicationWindow.UserFormatOrder.DisplayName_RealName_User => !string.IsNullOrEmpty(displayName) ? displayName : (!string.IsNullOrEmpty(realName) ? realName : userName),
                    ApplicationWindow.UserFormatOrder.User_DisplayName_RealName => !string.IsNullOrEmpty(userName) ? userName : (!string.IsNullOrEmpty(displayName) ? displayName : realName),
                    ApplicationWindow.UserFormatOrder.User_RealName_DisplayName => !string.IsNullOrEmpty(userName) ? userName : (!string.IsNullOrEmpty(realName) ? realName : displayName),
                    ApplicationWindow.UserFormatOrder.RealName_DisplayName_User => !string.IsNullOrEmpty(realName) ? realName : (!string.IsNullOrEmpty(displayName) ? displayName : userName),
                    ApplicationWindow.UserFormatOrder.RealName_User_DisplayName => !string.IsNullOrEmpty(realName) ? realName : (!string.IsNullOrEmpty(userName) ? userName : displayName),
                    _ => !string.IsNullOrEmpty(displayName) ? displayName : (!string.IsNullOrEmpty(userName) ? userName : realName),
                };
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ConvertUserToDisplayName() : {ex.Message}\n Returning as unknown user...\n"); });
                return "Unknown User";
            }
        }

        private static string ReplaceUserMentions(string messageText)
        {
            string replacedText = messageText;

            // Regular expression pattern to match user ID mentions
            string mentionPattern = @"<@(U[A-Z0-9]+)>";

            // Find all user ID mentions in the message text
            MatchCollection matches = Regex.Matches(messageText, mentionPattern);

            foreach (Match match in matches.Cast<Match>())
            {
                string userId = match.Groups[1].Value;

                // Find the corresponding user in the UsersDict dictionary
                if (UsersDict.TryGetValue(userId, out DeconstructedUser user))
                {
                    string replacementName = GetUserReplacementName(user);
                    replacedText = replacedText.Replace(match.Value, replacementName);
                }
            }

            return replacedText;
        }

        private static string GetUserReplacementName(DeconstructedUser user)
        {
            string displayName = user.Profile.DisplayName;
            string realName = user.Profile.RealName;
            string username = user.Name;

            return ApplicationWindow.CurrentUserFormatOrder switch
            {
                ApplicationWindow.UserFormatOrder.DisplayName_User_RealName => !string.IsNullOrEmpty(displayName) ? displayName : (!string.IsNullOrEmpty(username) ? username : realName),
                ApplicationWindow.UserFormatOrder.DisplayName_RealName_User => !string.IsNullOrEmpty(displayName) ? displayName : (!string.IsNullOrEmpty(realName) ? realName : username),
                ApplicationWindow.UserFormatOrder.User_DisplayName_RealName => !string.IsNullOrEmpty(username) ? username : (!string.IsNullOrEmpty(displayName) ? displayName : realName),
                ApplicationWindow.UserFormatOrder.User_RealName_DisplayName => !string.IsNullOrEmpty(username) ? username : (!string.IsNullOrEmpty(realName) ? realName : displayName),
                ApplicationWindow.UserFormatOrder.RealName_DisplayName_User => !string.IsNullOrEmpty(realName) ? realName : (!string.IsNullOrEmpty(displayName) ? displayName : username),
                ApplicationWindow.UserFormatOrder.RealName_User_DisplayName => !string.IsNullOrEmpty(realName) ? realName : (!string.IsNullOrEmpty(username) ? username : displayName),
                _ => !string.IsNullOrEmpty(displayName) ? displayName : (!string.IsNullOrEmpty(username) ? username : realName),
            };
        }

        private static string FormatMessage(string messageContent, string timestamp)
        {
            return $"[{timestamp}] : {messageContent}";
        }

        private static void SplitAndAddMessages(string formattedMessage, DeconstructedMessage deconstructedMessage, Channel channel, string userName, string userAvatar)
        {
            List<string> messageParts = SplitMessageIntoParts(formattedMessage);
            foreach (string part in messageParts)
            {
                ReconstructedMessage reconstructedMessage = new()
                {
                    User = userName,
                    Message = deconstructedMessage.Text,
                    Content = part,
                    ParentThreadTs = deconstructedMessage.ParentThreadTs,
                    ThreadType = deconstructedMessage.ThreadType,
                    IsPinned = deconstructedMessage.IsPinned,
                    Avatar = userAvatar,
                    OriginalTimestamp = deconstructedMessage.OriginalTimestamp
                };

                foreach (var url in deconstructedMessage.FileURLs)
                {
                    reconstructedMessage.FileURLs.Add(url);
                }

                channel.ReconstructedMessagesList.Add(reconstructedMessage);
            }
        }

        private static List<string> SplitMessageIntoParts(string message)
        {
            const int maxMessageLength = 2000;
            List<string> messageParts = [];
            Regex urlPattern = URLPattern();

            int currentIndex = 0;
            while (currentIndex < message.Length)
            {
                int bestSplitIndex = Math.Min(currentIndex + maxMessageLength, message.Length);

                // Ensure we're not splitting in the middle of a word.
                while (bestSplitIndex > currentIndex &&
                       bestSplitIndex < message.Length &&
                       !char.IsWhiteSpace(message[bestSplitIndex]))
                {
                    bestSplitIndex--;
                }

                // Check for URLs and ensure we're not splitting a URL.
                MatchCollection matches = urlPattern.Matches(message[currentIndex..bestSplitIndex]);
                if (matches.Count > 0)
                {
                    Match lastMatch = matches[^1];
                    if (lastMatch.Index + lastMatch.Length > bestSplitIndex)
                    {
                        bestSplitIndex = lastMatch.Index;
                    }
                }

                // Ensure we're not splitting in the middle of markdown syntax.
                int boldSyntax = message.LastIndexOf("**", bestSplitIndex - 2);
                if (boldSyntax > -1 && boldSyntax == bestSplitIndex - 2)
                {
                    bestSplitIndex -= 2;
                }

                // Use the range operator for substring
                string part = message[currentIndex..bestSplitIndex];
                messageParts.Add(part);

                currentIndex = bestSplitIndex;
            }

            return messageParts;
        }

        [GeneratedRegex(@"https?://\S+")]
        private static partial Regex URLPattern();
    }

    public class ReconstructedMessage
    {
        public string OriginalTimestamp { get; set; }
        public string User { get; set; }
        public string Avatar { get; set; }
        public string Message { get; set; }
        public string Content { get; set; }
        public string ParentThreadTs { get; set; }
        public ThreadType ThreadType { get; set; }
        public bool IsPinned { get; set; }
        public List<string> FileURLs { get; set; } = [];
        public List<string> FallbackFileURLs { get; set; } = [];
        public List<bool> IsFileDownloadable { get; set; } = [];
    }
}
