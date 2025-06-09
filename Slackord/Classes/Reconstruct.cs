using System.Text.RegularExpressions;
using Application = Microsoft.Maui.Controls.Application;

namespace Slackord.Classes
{
    /// <summary>
    /// Handles the reconstruction of Slack messages into Discord-compatible format
    /// </summary>
    public partial class Reconstruct
    {
        /// <summary>
        /// Dictionary of users indexed by user ID for message reconstruction
        /// </summary>
        internal static readonly Dictionary<string, DeconstructedUser> UsersDict = [];

        /// <summary>
        /// Dictionary to track thread information during reconstruction
        /// </summary>
        private static readonly Dictionary<string, ThreadInfo> threadDictionary = [];

        /// <summary>
        /// Gets a read-only view of the thread dictionary
        /// </summary>
        public static IReadOnlyDictionary<string, ThreadInfo> ThreadDictionary => threadDictionary;

        /// <summary>
        /// Initializes the users dictionary with parsed user data
        /// </summary>
        /// <param name="usersDict">Dictionary of user data from Slack export</param>
        public static void InitializeUsersDict(Dictionary<string, DeconstructedUser> usersDict)
        {
            foreach (KeyValuePair<string, DeconstructedUser> kvp in usersDict)
            {
                UsersDict[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Regular expression for matching bold text formatting
        /// </summary>
        [GeneratedRegex(@"\*\*((?:[^*]|(?:\*(?!\*)))*)\*\*")]
        private static partial Regex Bold();

        /// <summary>
        /// Regular expression for matching italic text formatting
        /// </summary>
        [GeneratedRegex(@"\*((?:[^*]|(?:\*(?!\*)))*)\*")]
        private static partial Regex Italics();

        /// <summary>
        /// Regular expression for matching underlined text formatting
        /// </summary>
        [GeneratedRegex(@"__((?:[^_]|(?:_(?!_)))*)__")]
        private static partial Regex Underline();

        /// <summary>
        /// Regular expression for matching strikethrough text formatting
        /// </summary>
        [GeneratedRegex(@"~~((?:[^~]|(?:~(?!~)))*)~~")]
        private static partial Regex Strikethrough();

        /// <summary>
        /// Regular expression for matching masked links in Slack format
        /// </summary>
        [GeneratedRegex(@"<(https?://[^|]+)\|(.*?)>")]
        private static partial Regex MaskedLinks();

        /// <summary>
        /// Regular expression for matching block quotes
        /// </summary>
        [GeneratedRegex(@"&gt; (.+?)(?=\n|$)")]
        private static partial Regex BlockQuotes();

        /// <summary>
        /// Reconstructs all messages across multiple channels with progress tracking
        /// </summary>
        /// <param name="channels">List of channels to reconstruct</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        public static async Task ReconstructAsync(List<Channel> channels, CancellationToken cancellationToken)
        {
            try
            {
                ProcessingManager.Instance.SetState(ProcessingState.ReconstructingMessages);
                ApplicationWindow.ShowProgressBar();

                int totalMessages = channels.Sum(c => c.DeconstructedMessagesList.Count);
                int processedMessages = 0;

                DateTime lastProgressUpdate = DateTime.Now;
                const int progressUpdateIntervalSeconds = 15;

                Application.Current.Dispatcher.Dispatch(() =>
                {
                    ApplicationWindow.WriteToDebugWindow($"Starting reconstruction of {totalMessages:N0} messages across {channels.Count} channels\n");
                });

                for (int channelIndex = 0; channelIndex < channels.Count; channelIndex++)
                {
                    Channel channel = channels[channelIndex];

                    Application.Current.Dispatcher.Dispatch(() =>
                    {
                        ApplicationWindow.WriteToDebugWindow($"Processing channel {channelIndex + 1}/{channels.Count}: {channel.Name} " +
                            $"({channel.DeconstructedMessagesList.Count} messages)\n");
                    });

                    for (int i = 0; i < channel.DeconstructedMessagesList.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        DeconstructedMessage deconstructedMessage = channel.DeconstructedMessagesList[i];
                        await ReconstructMessage(deconstructedMessage, channel);

                        processedMessages++;

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
                    }

                    Application.Current.Dispatcher.Dispatch(() =>
                    {
                        ApplicationWindow.WriteToDebugWindow($"✅ Completed channel: {channel.Name} - {channel.ReconstructedMessagesList.Count:N0} messages reconstructed\n");
                    });
                }

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

        /// <summary>
        /// Reconstructs a single Slack message into Discord-compatible format
        /// </summary>
        /// <param name="deconstructedMessage">The deconstructed Slack message</param>
        /// <param name="channel">The channel this message belongs to</param>
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
                            var (localFilePath, permalink) = await DownloadFile(fileUrl, channel.Name, isDownloadable);
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

        /// <summary>
        /// Downloads a file from Slack and saves it locally
        /// </summary>
        /// <param name="fileUrl">The URL of the file to download</param>
        /// <param name="channelName">The name of the channel (for organizing downloads)</param>
        /// <param name="isDownloadable">Whether the file is downloadable</param>
        /// <returns>A tuple containing the local file path and permalink</returns>
        public static async Task<(string localFilePath, string permalink)> DownloadFile(string fileUrl, string channelName, bool isDownloadable)
        {
            if (!isDownloadable || string.IsNullOrWhiteSpace(fileUrl))
            {
                return (null, null);
            }

            string downloadsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads", channelName);

            Directory.CreateDirectory(downloadsFolder);

            if (File.Exists(fileUrl))
            {
                string localFileName = Path.GetFileName(fileUrl);
                string sanitizedLocalFileName = string.Concat(localFileName.Split(Path.GetInvalidFileNameChars()));
                string destinationPath = Path.Combine(downloadsFolder, sanitizedLocalFileName);

                if (File.Exists(destinationPath))
                {
                    string fileExtension = Path.GetExtension(sanitizedLocalFileName);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sanitizedLocalFileName);
                    destinationPath = Path.Combine(downloadsFolder, $"{fileNameWithoutExtension}_{Guid.NewGuid()}{fileExtension}");
                }

                File.Copy(fileUrl, destinationPath);
                return (destinationPath, fileUrl);
            }

            if (!Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute))
            {
                Logger.Log($"Invalid URL provided: {fileUrl}");
                return (null, null);
            }

            string fileName = Path.GetFileName(new Uri(fileUrl).LocalPath);
            string sanitizedFileName = string.Concat(fileName.Split(Path.GetInvalidFileNameChars()));
            string localFilePath = Path.Combine(downloadsFolder, sanitizedFileName);

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

        /// <summary>
        /// Converts a Unix timestamp string to a localized date/time string
        /// </summary>
        /// <param name="timestampString">The Unix timestamp string to convert</param>
        /// <returns>A formatted local date/time string</returns>
        private static string ConvertTimestampToLocalizedString(string timestampString)
        {
            try
            {
                string[] parts = timestampString.Split('.');
                if (parts.Length != 2)
                {
                    throw new ArgumentException("Invalid timestamp format");
                }

                long wholeSeconds = long.Parse(parts[0]);
                long fractionalTicks = long.Parse(parts[1]) * (TimeSpan.TicksPerSecond / 1_000_000);

                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(wholeSeconds).AddTicks(fractionalTicks);

                string timestampValue = Preferences.Default.Get("TimestampValue", "12 Hour");

                string format = timestampValue == "24 Hour" ? "yyyy-MM-dd HH:mm:ss" : "yyyy-MM-dd hh:mm:ss tt";

                return dateTimeOffset.ToLocalTime().ToString(format);
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ConvertTimestampToLocalizedString() : {ex.Message}\n"); });
                return null;
            }
        }

        /// <summary>
        /// Converts Slack formatting to Discord markdown
        /// </summary>
        /// <param name="input">The text with Slack formatting</param>
        /// <returns>Text with Discord markdown formatting</returns>
        private static string ConvertToDiscordMarkdown(string input)
        {
            try
            {
                input = Bold().Replace(input, "**$1**");
                input = Italics().Replace(input, "*$1*");
                input = Underline().Replace(input, "__$1__");
                input = Strikethrough().Replace(input, "~~$1~~");
                input = MaskedLinks().Replace(input, "[$2]($1)");
                input = BlockQuotes().Replace(input, "> $1\n");

                return input;
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ConvertToDiscordMarkdown() : {ex.Message}\n"); });
                return input;
            }
        }

        /// <summary>
        /// Converts a user ID to a display name based on current format settings
        /// </summary>
        /// <param name="userId">The Slack user ID</param>
        /// <returns>The formatted display name</returns>
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

        /// <summary>
        /// Replaces Slack user mentions with formatted names
        /// </summary>
        /// <param name="messageText">The message text containing user mentions</param>
        /// <returns>Message text with replaced user mentions</returns>
        private static string ReplaceUserMentions(string messageText)
        {
            string replacedText = messageText;

            string mentionPattern = @"<@(U[A-Z0-9]+)>";

            MatchCollection matches = Regex.Matches(messageText, mentionPattern);

            foreach (Match match in matches.Cast<Match>())
            {
                string userId = match.Groups[1].Value;

                if (UsersDict.TryGetValue(userId, out DeconstructedUser user))
                {
                    string replacementName = GetUserReplacementName(user);
                    replacedText = replacedText.Replace(match.Value, replacementName);
                }
            }

            return replacedText;
        }

        /// <summary>
        /// Gets the replacement name for a user mention based on format settings
        /// </summary>
        /// <param name="user">The deconstructed user object</param>
        /// <returns>The formatted replacement name</returns>
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

        /// <summary>
        /// Formats a message with timestamp
        /// </summary>
        /// <param name="messageContent">The message content</param>
        /// <param name="timestamp">The formatted timestamp</param>
        /// <returns>The formatted message string</returns>
        private static string FormatMessage(string messageContent, string timestamp)
        {
            return $"[{timestamp}] : {messageContent}";
        }

        /// <summary>
        /// Splits long messages and adds them to the channel's reconstructed messages list
        /// </summary>
        /// <param name="formattedMessage">The formatted message to split if necessary</param>
        /// <param name="deconstructedMessage">The original deconstructed message</param>
        /// <param name="channel">The channel to add messages to</param>
        /// <param name="userName">The user name for the message</param>
        /// <param name="userAvatar">The user avatar URL</param>
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

        /// <summary>
        /// Splits a message into parts that fit Discord's character limits
        /// </summary>
        /// <param name="message">The message to split</param>
        /// <returns>A list of message parts</returns>
        private static List<string> SplitMessageIntoParts(string message)
        {
            const int maxMessageLength = 2000;
            List<string> messageParts = [];
            Regex urlPattern = URLPattern();

            int currentIndex = 0;
            while (currentIndex < message.Length)
            {
                int bestSplitIndex = Math.Min(currentIndex + maxMessageLength, message.Length);

                while (bestSplitIndex > currentIndex &&
                       bestSplitIndex < message.Length &&
                       !char.IsWhiteSpace(message[bestSplitIndex]))
                {
                    bestSplitIndex--;
                }

                MatchCollection matches = urlPattern.Matches(message[currentIndex..bestSplitIndex]);
                if (matches.Count > 0)
                {
                    Match lastMatch = matches[^1];
                    if (lastMatch.Index + lastMatch.Length > bestSplitIndex)
                    {
                        bestSplitIndex = lastMatch.Index;
                    }
                }

                int boldSyntax = message.LastIndexOf("**", bestSplitIndex - 2);
                if (boldSyntax > -1 && boldSyntax == bestSplitIndex - 2)
                {
                    bestSplitIndex -= 2;
                }

                string part = message[currentIndex..bestSplitIndex];
                messageParts.Add(part);

                currentIndex = bestSplitIndex;
            }

            return messageParts;
        }

        /// <summary>
        /// Regular expression for matching URLs
        /// </summary>
        [GeneratedRegex(@"https?://\S+")]
        private static partial Regex URLPattern();
    }

    /// <summary>
    /// Represents a reconstructed message ready for Discord posting
    /// </summary>
    public class ReconstructedMessage
    {
        /// <summary>
        /// Gets or sets the original timestamp from Slack
        /// </summary>
        public string OriginalTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the user name for display
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Gets or sets the user avatar URL
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// Gets or sets the original message content
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the formatted content ready for Discord
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the parent thread timestamp
        /// </summary>
        public string ParentThreadTs { get; set; }

        /// <summary>
        /// Gets or sets the thread type (None, Parent, Reply)
        /// </summary>
        public ThreadType ThreadType { get; set; }

        /// <summary>
        /// Gets or sets whether this message should be pinned
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// Gets or sets the list of local file paths for attachments
        /// </summary>
        public List<string> FileURLs { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of fallback file URLs
        /// </summary>
        public List<string> FallbackFileURLs { get; set; } = [];

        /// <summary>
        /// Gets or sets the list indicating which files are downloadable
        /// </summary>
        public List<bool> IsFileDownloadable { get; set; } = [];
    }
}