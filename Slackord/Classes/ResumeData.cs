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

        public static async Task InitializeChannelForResume(Channel channel, ResumeData resumeData)
        {
            // Set the Discord channel ID based on resume data
            channel.DiscordChannelId = resumeData.ChannelIdUlong;

            // Proceed with reconstruction and posting if needed
            if (resumeData.Reconstructed)
            {
                // Messages are already deconstructed and ready to be reconstructed
                await ReconstructMessages(channel, resumeData);
            }
            else
            {
                // Deconstruct messages first
                DeconstructMessages(channel);
                resumeData.Reconstructed = true;
                SaveResumeData(LoadResumeData()); // Save the updated state
                await ReconstructMessages(channel, resumeData);
            }
        }

        private static void DeconstructMessages(Channel channel)
        {
            // Fetch messages and deconstruct them
            foreach (var messageJson in FetchMessagesJsonForChannel(channel.Name))
            {
                DeconstructedMessage deconstructedMessage = Deconstruct.DeconstructMessage(messageJson);
                channel.DeconstructedMessagesList.Add(deconstructedMessage);
            }
        }

        private static async Task ReconstructMessages(Channel channel, ResumeData resumeData)
        {
            int startPosition = resumeData.LastMessagePosition + 1;
            ApplicationWindow.UpdateProgressBar(startPosition, channel.DeconstructedMessagesList.Count, "messages");

            for (int i = startPosition; i < channel.DeconstructedMessagesList.Count; i++)
            {
                // Process each message
                var deconstructedMessage = channel.DeconstructedMessagesList[i];
                await Reconstruct.ReconstructMessage(deconstructedMessage, channel);

                // Update the resume state
                resumeData.LastMessagePosition = i;
                SaveResumeData(LoadResumeData());
            }

            // Mark as complete if all messages are posted
            if (resumeData.LastMessagePosition == channel.DeconstructedMessagesList.Count - 1)
            {
                resumeData.ImportedToDiscord = true;
                SaveResumeData(LoadResumeData());
            }
        }

        private static List<JObject> FetchMessagesJsonForChannel(string channelName)
        {
            // Logic to fetch raw JSON messages for the channel
            return new List<JObject>(); // Placeholder
        }
    }
}
