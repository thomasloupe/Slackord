using MenuApp;
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

        [GeneratedRegex("<i>(.*?)</i>")]
        private static partial Regex Italics();

        [GeneratedRegex("<b>(.*?)</b>")]
        private static partial Regex Bold();

        [GeneratedRegex("<u>(.*?)</u>")]
        private static partial Regex Underline();

        [GeneratedRegex("<s>(.*?)</s>")]
        private static partial Regex Strikethrough();

        [GeneratedRegex("<a href=\"(.*?)\">(.*?)</a>")]
        private static partial Regex MaskedLinks();

        public static async Task ReconstructAsync(List<Channel> channels, CancellationToken cancellationToken)
        {
            try
            {
                // Iterate through each channel.
                foreach (var channel in channels)
                {
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Processing channel: {channel.Name}, DeconstructedMessagesCount: {channel.DeconstructedMessagesList.Count}\n"); });

                    // Iterate through each deconstructed message in the channel.
                    foreach (var deconstructedMessage in channel.DeconstructedMessagesList)
                    {
                        // Reconstruct message for Discord.
                        ReconstructMessage(deconstructedMessage, channel);
                    }

                    Application.Current.Dispatcher.Dispatch(() =>
                    {
                        ApplicationWindow.WriteToDebugWindow($"ReconstructedMessagesCount after processing: {channel.ReconstructedMessagesList.Count}\n");
                    });
                }
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
                    // Handle the error, e.g., log it, and don't continue processing the message.
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Invalid timestamp format: {deconstructedMessage.Timestamp}\n"); });
                    return;
                }
                long timestampMilliseconds = (long)(timestampDouble * 1000);
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMilliseconds).ToString("dd-MM-yy");

                // Handle rich text formatting.
                var messageContent = deconstructedMessage.Text;
                messageContent = ConvertToDiscordMarkdown(messageContent);

                // Handle getting the username for the message.
                var formattedMessage = string.Empty;
                if (UsersDict.TryGetValue(deconstructedMessage.User, out DeconstructedUser user))
                {
                    string userName =
                        !string.IsNullOrEmpty(user.DisplayName) ? user.DisplayName :
                        !string.IsNullOrEmpty(user.Name) ? user.Name :
                        !string.IsNullOrEmpty(user.RealName) ? user.RealName :
                        user.Id;  // Default to the user's ID if no name is available.

                    // Format the message.
                    formattedMessage = $"[{timestamp}] {userName} - {messageContent}";
                }
                else
                {
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"User not found: {deconstructedMessage.User}\n"); });
                }

                // Ensure formattedMessage is not empty before proceeding to split and reconstruct the message
                if (!string.IsNullOrEmpty(formattedMessage))
                {
                    var messageParts = SplitMessageIntoParts(formattedMessage);  // Declare messageParts here
                    foreach (var part in messageParts)
                    {
                        var reconstructedMessage = new ReconstructedMessage { Content = part };
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
                input = Italics().Replace(input, "*$1*");  // Italics
                input = Bold().Replace(input, "**$1**");  // Bold
                input = Underline().Replace(input, "__$1__");  // Underline
                input = Strikethrough().Replace(input, "~~$1~~");  // Strikethrough
                input = MaskedLinks().Replace(input, "[$2]($1)");  // Masked Links
                                                                   // Additional conversions here.

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
        // Define properties based on Discord message structure.
        public string Content { get; set; }
        // Additional properties for Discord messages.
    }
}
