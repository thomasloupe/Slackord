using MenuApp;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Slackord.Classes
{
    public partial class Reconstruct
    {
        private static readonly Dictionary<string, DeconstructedUser> UsersDict = new();

        public static void InitializeUsersDict(Dictionary<string, DeconstructedUser> usersDict)
        {
            foreach (var kvp in usersDict)
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
                foreach (var channel in channels)
                {
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Deconstructing {channel.DeconstructedMessagesList.Count} messages for {channel.Name}\n"); });

                    // Iterate through each deconstructed message in the channel.
                    foreach (var deconstructedMessage in channel.DeconstructedMessagesList)
                    {
                        // Reconstruct message for Discord.
                        ReconstructMessage(deconstructedMessage, channel);
                    }

                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Reconstructed {channel.ReconstructedMessagesList.Count} messages for {channel.Name}\n"); });
                }
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"All channels have been successfully deconstructed and reconstructed for Discord!\n"); });
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ReconstructAsync(): {ex.Message}\n"); });
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
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Invalid timestamp format: {deconstructedMessage.Timestamp}\n"); });
                    return;
                }
                long timestampMilliseconds = (long)(timestampDouble * 1000);
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestampMilliseconds);

                // Get the current culture's DateTimeFormatInfo object.
                DateTimeFormatInfo dtfi = CultureInfo.CurrentCulture.DateTimeFormat;

                // Create a custom format string using the current culture's short date pattern and long time pattern.
                string customFormat = $"{dtfi.ShortDatePattern} {dtfi.LongTimePattern}";

                // Format the DateTimeOffset object using the custom format string.
                var timestamp = dateTimeOffset.ToString(customFormat);

                // Handle rich text formatting.
                var messageContent = deconstructedMessage.Text;
                messageContent = ConvertToDiscordMarkdown(messageContent);

                // Handle getting the username for the message.
                var formattedMessage = string.Empty;
                if (UsersDict.TryGetValue(deconstructedMessage.User, out DeconstructedUser user))
                {
                    string userName =
                        !string.IsNullOrEmpty(user.Profile.DisplayName) ? user.Profile.DisplayName :
                        !string.IsNullOrEmpty(user.Name) ? user.Name :
                        !string.IsNullOrEmpty(user.Profile.RealName) ? user.Profile.RealName :
                        user.Id;  // Default to the user's ID if no name is available.

                    // Format the message.
                    formattedMessage = $"[{timestamp}] {userName}: {messageContent}";
                }
                else
                {
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"User not found: {deconstructedMessage.User}\n"); });
                }

                // Ensure formattedMessage is not empty before proceeding to split and reconstruct the message.
                if (!string.IsNullOrEmpty(formattedMessage))
                {
                    var messageParts = SplitMessageIntoParts(formattedMessage);
                    foreach (var part in messageParts)
                    {
                        var reconstructedMessage = new ReconstructedMessage
                        {
                            Content = part,
                            ParentThreadTs = deconstructedMessage.ParentThreadTs,
                            ThreadType = deconstructedMessage.ThreadType,
                            IsPinned = deconstructedMessage.IsPinned
                        };
                        channel.ReconstructedMessagesList.Add(reconstructedMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ReconstructMessage(): {ex.Message}\n"); });
            }
        }

        private static string ConvertToDiscordMarkdown(string input)
        {
            try
            {
                // Rich text conversions.
                input = Bold().Replace(input, "**$1**");       // Bold
                input = Italics().Replace(input, "*$1*");      // Italics
                input = Underline().Replace(input, "__$1__");  // Underline
                input = Strikethrough().Replace(input, "~~$1~~"); // Strikethrough
                input = MaskedLinks().Replace(input, "[$2]($1)");  // Masked Links
                input = BlockQuotes().Replace(input, "> $1\n");    // Blockquotes

                return input;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ConvertToDiscordMarkdown(): {ex.Message}\n"); });
                return input;
            }
        }

        private static List<string> SplitMessageIntoParts(string message)
        {
            const int maxMessageLength = 2000;
            var messageParts = new List<string>();

            int index = 0;
            while (index < message.Length)
            {
                int partLength = Math.Min(maxMessageLength, message.Length - index);
                var messagePart = message.Substring(index, partLength);
                messageParts.Add(messagePart);
                index += partLength;
            }

            return messageParts;
        }
    }

    public class ReconstructedMessage
    {
        public string Content { get; set; }
        public string ParentThreadTs { get; set; }
        public ThreadType ThreadType { get; set; }
        public bool IsPinned { get; set; }
    }
}
