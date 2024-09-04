using MenuApp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Slackord.Classes
{
    public class ResumeData
    {
        public string ChannelName { get; set; }
        public ulong ChannelIdUlong { get; set; }
        public bool Reconstructed { get; set; }
        public bool ImportedToDiscord { get; set; }
        public int LastMessagePosition { get; set; }

        public static List<ResumeData> LoadResumeData()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Slackord_Resume_Data.json");
            if (!File.Exists(filePath))
            {
                return new List<ResumeData>();
            }

            string jsonData = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<ResumeData>>(jsonData);
        }

        public static void SaveResumeData(List<ResumeData> data)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Slackord_Resume_Data.json");
            string jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, jsonData);
        }

        public static async Task<bool> AskUserToResumeChannel(string channelName)
        {
            return await MainPage.Current.DisplayAlert("Resume Import", $"This channel '{channelName}' has not finished importing to Discord. Would you like to resume?", "Yes", "No");
        }

        public static void InitializeChannelForResume(Channel channel, ResumeData resumeData)
        {
            // Set the Discord channel ID based on resume data
            channel.DiscordChannelId = resumeData.ChannelIdUlong;

            // Check if the channel has already been reconstructed
            if (resumeData.Reconstructed)
            {
                // Check if the channel's deconstructed messages list is populated
                if (channel.DeconstructedMessagesList == null || channel.DeconstructedMessagesList.Count == 0)
                {
                    // Reload or reinitialize the list of deconstructed messages if needed
                    channel.DeconstructedMessagesList = LoadDeconstructedMessagesForChannel(channel.Name);
                }
            }
            else
            {
                // Deconstruct messages if not already reconstructed
                channel.DeconstructedMessagesList = new List<DeconstructedMessage>();
                foreach (var messageJson in FetchMessagesJsonForChannel(channel.Name))
                {
                    DeconstructedMessage deconstructedMessage = Deconstruct.DeconstructMessage(messageJson);
                    channel.DeconstructedMessagesList.Add(deconstructedMessage);
                }
                resumeData.Reconstructed = true; // Mark as reconstructed
            }

            // Check if the channel was partially posted to Discord
            if (!resumeData.ImportedToDiscord)
            {
                // Calculate the start position to the last successfully posted message + 1
                int startPosition = resumeData.LastMessagePosition + 1;

                // Logic to handle setting up the state for importing remaining messages
                // Reset progress bar or any relevant UI elements
                ApplicationWindow.ResetProgressBar();
                ApplicationWindow.UpdateProgressBar(startPosition, channel.DeconstructedMessagesList.Count, "messages");

                // Use startPosition to resume from the correct place in the DeconstructedMessagesList
                // This might be passed to a method that starts posting messages to Discord
            }
        }

        private static List<DeconstructedMessage> LoadDeconstructedMessagesForChannel(string _channelName)
        {
            // Implement logic to load deconstructed messages from storage or regenerate them
            return new List<DeconstructedMessage>(); // Placeholder
        }

        private static List<JObject> FetchMessagesJsonForChannel(string _channelName)
        {
            // Implement logic to fetch raw JSON messages for the channel
            return new List<JObject>(); // Placeholder
        }
    }
}
