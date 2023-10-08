using MenuApp;
using Octokit;
using System.Globalization;
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
                    _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Deconstructing {channel.DeconstructedMessagesList.Count} messages for {channel.Name}\n"); });

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
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ReconstructAsync(): {ex.Message}\n"); });
            }
        }

        private static void ReconstructMessage(DeconstructedMessage deconstructedMessage, Channel channel)
        {
            try
            {
                // Convert Unix timestamp to a readable format.
                if (!double.TryParse(deconstructedMessage.Timestamp, out double timestampDouble))
                {
                    // Log the error, and don't continue processing the message.
                    _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Invalid timestamp format: {deconstructedMessage.Timestamp}\n"); });
                    return;
                }
                long timestampMilliseconds = (long)(timestampDouble * 1000);
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestampMilliseconds);

                // Get the current culture's DateTimeFormatInfo object.
                DateTimeFormatInfo dtfi = CultureInfo.CurrentCulture.DateTimeFormat;

                // Create a custom format string using the current culture's short date pattern and long time pattern.
                string customFormat = $"{dtfi.ShortDatePattern} {dtfi.LongTimePattern}";

                // Format the DateTimeOffset object using the custom format string.
                string timestamp = dateTimeOffset.ToString(customFormat);

                // Handle rich text formatting.
                string messageContent = deconstructedMessage.Text;
                messageContent = ConvertToDiscordMarkdown(messageContent);

                // Handle getting the username and avatar for the message.
                string formattedMessage = string.Empty;
                string userAvatar = null;
                string userName = string.Empty;
                if (UsersDict.TryGetValue(deconstructedMessage.User, out DeconstructedUser user))
                {
                    userName =
                        !string.IsNullOrEmpty(user.Profile.DisplayName) ? user.Profile.DisplayName :
                        !string.IsNullOrEmpty(user.Name) ? user.Name :
                        !string.IsNullOrEmpty(user.Profile.RealName) ? user.Profile.RealName :
                        user.Id;  // Default to the user's ID if no name is available.

                    userAvatar = user.Profile.Avatar; // Get the avatar from the user profile.

                    // Format the message.
                    formattedMessage = $"[{timestamp}] : {messageContent}";
                }
                else
                {
                    _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"User not found: {deconstructedMessage.User}\n"); });
                }

                // Ensure formattedMessage is not empty before proceeding to split and reconstruct the message.
                if (!string.IsNullOrEmpty(formattedMessage))
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
                            FileURLs = deconstructedMessage.FileURLs,
                            Avatar = userAvatar
                        };
                        channel.ReconstructedMessagesList.Add(reconstructedMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ReconstructMessage(): {ex.Message}\n"); });
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
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ConvertToDiscordMarkdown(): {ex.Message}\n"); });
                return input;
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
                    Match lastMatch = matches[^1]; // Use the ^ operator for the last element
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
        public string User { get; set; }
        public string Avatar { get; set; }
        public string Message { get; set; }
        public string Content { get; set; }
        public string ParentThreadTs { get; set; }
        public ThreadType ThreadType { get; set; }
        public bool IsPinned { get; set; }
        public List<string> FileURLs { get; set; } = new List<string>();
    }
}
