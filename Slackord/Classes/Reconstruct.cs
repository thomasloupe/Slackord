using MenuApp;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Application = Microsoft.Maui.Controls.Application;

namespace Slackord.Classes
{
    public partial class Reconstruct
    {
        internal static readonly Dictionary<string, DeconstructedUser> UsersDict = new();

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
                // Iterate through each channel.
                foreach (Channel channel in channels)
                {
                    _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"\nDeconstructing {channel.DeconstructedMessagesList.Count} messages for {channel.Name}\n"); });

                    // Iterate through each deconstructed message in the channel.
                    foreach (DeconstructedMessage deconstructedMessage in channel.DeconstructedMessagesList)
                    {
                        // Reconstruct message for Discord.
                        ReconstructMessage(deconstructedMessage, channel);
                    }

                    _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Reconstructed {channel.ReconstructedMessagesList.Count} messages for {channel.Name}\n"); });
                }
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"All channels have been successfully deconstructed and reconstructed for Discord!\n"); });
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ReconstructAsync(): {ex.Message}\n\n"); });
            }
        }

        private static void ReconstructMessage(DeconstructedMessage deconstructedMessage, Channel channel)
        {
            try
            {
                string messageContent = string.IsNullOrEmpty(deconstructedMessage.Text) ? string.Empty : ConvertToDiscordMarkdown(deconstructedMessage.Text);

                // Check for files and their downloadability.
                if (deconstructedMessage.FileURLs.Count > 0)
                {
                    for (int i = 0; i < deconstructedMessage.FileURLs.Count; i++)
                    {
                        string fileUrl = deconstructedMessage.FileURLs[i];
                        bool isDownloadable = deconstructedMessage.IsFileDownloadable[i];

                        if (isDownloadable)
                        {
                            // Call DownloadFile method.
                            Task.Run(() => DownloadFile(fileUrl, channel.Name, deconstructedMessage.OriginalTimestamp, isDownloadable))
                            .ContinueWith(t =>
                            {
                                if (t.IsCompletedSuccessfully)
                                {
                                    var (localFilePath, permalink) = t.Result;

                                    // Find the corresponding ReconstructedMessage.
                                    var reconstructedMessage = channel.ReconstructedMessagesList.FirstOrDefault(rm => rm.OriginalTimestamp == deconstructedMessage.OriginalTimestamp);

                                    // Add the localFilePath and permalink to the ReconstructedMessage.
                                    reconstructedMessage?.FileURLs.Enqueue(localFilePath);
                                }
                                else if (t.IsFaulted)
                                {
                                    Logger.Log($"File download for channel {channel.Name} failed! Original Slack Message:\n{deconstructedMessage.OriginalSlackMessageJson}");
                                }
                            });
                        }
                        else
                        {
                            // File is hidden by Slack, append this info to the messageContent.
                            messageContent += " [File hidden by Slack limit]";
                        }
                    }
                }

                // If after all of this, the message is still empty, set a default message.
                if (string.IsNullOrEmpty(messageContent))
                {
                    messageContent = "File hidden by Slack limit";
                }

                string timestampString = deconstructedMessage.Timestamp?.ToString();
                if (string.IsNullOrEmpty(timestampString))
                {
                    // Log invalid or missing timestamp.
                    Logger.Log(timestampString == null ? $"Missing timestamp for message {deconstructedMessage.Text}" : $"Invalid timestamp format {deconstructedMessage.Text}");
                    return;
                }

                string displayTimestamp = ConvertTimestampToLocalizedString(timestampString);
                if (string.IsNullOrEmpty(displayTimestamp))
                {
                    Logger.Log($"Invalid timestamp format {deconstructedMessage.Text}");
                    return;
                }

                string userName = ConvertUserToDisplayName(deconstructedMessage.User);
                string userAvatar = deconstructedMessage.User != null && UsersDict.TryGetValue(deconstructedMessage.User, out DeconstructedUser user) ? user.Profile.Avatar : null;

                string formattedMessage = FormatMessage(messageContent, displayTimestamp, userName);
                SplitAndAddMessages(formattedMessage, deconstructedMessage, channel, userName, userAvatar);
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ReconstructMessage() : {ex.Message}\n"); });
            }
        }

        public static async Task<(string localFilePath, string permalink)> DownloadFile(string fileUrl, string channelName, string originalTimestamp, bool isDownloadable)
        {
            if (!isDownloadable)
            {
                return (null, null);
            }

            try
            {
                // Check for null or empty originalTimestamp.
                if (string.IsNullOrEmpty(originalTimestamp))
                {
                    originalTimestamp = Guid.NewGuid().ToString();
                }

                // Check if the fileUrl is a slackdump-style local path.
                if (fileUrl.StartsWith("attachments/"))
                {
                    // This is a slackdump-style local path (indicating it's a slackdump export).
                    string channelFolder = Path.Combine(ImportJson.RootFolderPath, channelName);
                    string localFilePath = Path.Combine(channelFolder, "attachments", fileUrl["attachments/".Length..]);

                    if (File.Exists(localFilePath))
                    {
                        return (localFilePath, fileUrl); // Return the local path.
                    }
                    else
                    {
                        // Log that the file doesn't exist locally.
                        Logger.Log($"Expected file from Slackdump doesn't exist locally: {localFilePath}");
                        return (null, null);
                    }
                }

                // Continue with the rest of the method for normal Slack export.
                if (!Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute))
                {
                    Logger.Log($"Invalid URL provided: {fileUrl}");
                    return (null, null);
                }

                Logger.Log($"Attempting to download from URL: {fileUrl}");

                using HttpClient httpClient = new();
                HttpResponseMessage response = await httpClient.GetAsync(fileUrl);

                if (response.IsSuccessStatusCode)
                {
                    byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                    string downloadsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Downloads");
                    Directory.CreateDirectory(downloadsFolder);
                    string channelFolder = Path.Combine(downloadsFolder, channelName);
                    Directory.CreateDirectory(channelFolder);
                    string fileExtension = Path.GetExtension(new Uri(fileUrl).AbsolutePath);
                    string fileName = $"{originalTimestamp}{fileExtension}";
                    string localFilePath = Path.Combine(channelFolder, fileName);

                    if (File.Exists(localFilePath))
                    {
                        fileName = $"{originalTimestamp}_{Guid.NewGuid()}{fileExtension}";
                        localFilePath = Path.Combine(channelFolder, fileName);
                    }

                    await File.WriteAllBytesAsync(localFilePath, fileBytes);
                    string permalink = fileUrl;

                    return (localFilePath, permalink);
                }
                else
                {
                    _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Failed to download file: {response.StatusCode}\n"); });
                    return (null, null);
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"DownloadFile() : {ex.Message}\n"); });
                return (null, null);
            }
        }

        private static string ConvertTimestampToLocalizedString(string timestampString)
        {
            try
            {
                // Split the timestamp into whole and fractional seconds
                string[] parts = timestampString.Split('.');
                if (parts.Length != 2)
                {
                    throw new ArgumentException("Invalid timestamp format");
                }

                long wholeSeconds = long.Parse(parts[0]);
                long fractionalTicks = long.Parse(parts[1]) * (TimeSpan.TicksPerSecond / 1_000_000); // Convert microseconds to ticks

                // Create a DateTimeOffset from the Unix timestamp
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(wholeSeconds).AddTicks(fractionalTicks);

                // Retrieve the current timestamp setting
                string timestampValue = Preferences.Default.Get("TimestampValue", "12 Hour");

                // Determine the format string
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
            if (userId != null && UsersDict.TryGetValue(userId, out DeconstructedUser user))
            {
                return !string.IsNullOrEmpty(user.Profile.DisplayName) ? user.Profile.DisplayName :
                       !string.IsNullOrEmpty(user.Name) ? user.Name :
                       !string.IsNullOrEmpty(user.Profile.RealName) ? user.Profile.RealName :
                       user.Id;
            }
            return "Unknown User";
        }

        private static string FormatMessage(string messageContent, string timestamp, string userName)
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

                // Add each item in the list to the bag.
                foreach (var url in deconstructedMessage.FileURLs)
                {
                    reconstructedMessage.FileURLs.Enqueue(url);
                }

                channel.ReconstructedMessagesList.Enqueue(reconstructedMessage);
            }
        }

        private static List<string> SplitMessageIntoParts(string message)
        {
            const int maxMessageLength = 2000;
            List<string> messageParts = new();
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

                // Ensure we're not splitting in the middle of markdown syntax (this can be expanded as needed).
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
        public ConcurrentQueue<string> FileURLs { get; set; } = new ConcurrentQueue<string>();
        public List<string> FallbackFileURLs { get; set; } = new List<string>();
        public List<bool> IsFileDownloadable { get; set; } = new List<bool>();
    }
}
