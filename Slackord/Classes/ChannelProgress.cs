using Newtonsoft.Json;

namespace Slackord.Classes
{
    /// <summary>
    /// Tracks progress for a single channel during import
    /// </summary>
    public class ChannelProgress
    {
        /// <summary>
        /// Original Slack channel name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Discord channel ID once created (0 if not created yet)
        /// </summary>
        public ulong DiscordChannelId { get; set; }

        /// <summary>
        /// Total number of messages to import for this channel
        /// </summary>
        public int TotalMessages { get; set; }

        /// <summary>
        /// Number of messages successfully sent to Discord
        /// </summary>
        public int MessagesSent { get; set; }

        /// <summary>
        /// Whether this channel has completed importing
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Timestamp of the last successfully sent message
        /// </summary>
        public string LastMessageTimestamp { get; set; }

        /// <summary>
        /// Discord channel description/topic
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether the .slackord file has been created for this channel
        /// </summary>
        public bool FileCreated { get; set; }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        [JsonIgnore]
        public double ProgressPercentage => TotalMessages > 0 ? (double)MessagesSent / TotalMessages * 100 : 0;

        /// <summary>
        /// Whether this channel needs to be created on Discord
        /// </summary>
        [JsonIgnore]
        public bool NeedsDiscordChannel => DiscordChannelId == 0;

        /// <summary>
        /// Number of messages remaining to send
        /// </summary>
        [JsonIgnore]
        public int MessagesRemaining => Math.Max(0, TotalMessages - MessagesSent);

        /// <summary>
        /// Updates the progress when a message is successfully sent
        /// </summary>
        public void RecordMessageSent(string messageTimestamp)
        {
            MessagesSent++;
            LastMessageTimestamp = messageTimestamp;

            if (MessagesSent >= TotalMessages)
            {
                IsCompleted = true;
            }
        }

        /// <summary>
        /// Sets the Discord channel ID when channel is created
        /// </summary>
        public void SetDiscordChannelId(ulong channelId)
        {
            DiscordChannelId = channelId;
        }

        /// <summary>
        /// Gets a display string for UI showing progress
        /// </summary>
        public string GetProgressDisplay()
        {
            if (IsCompleted)
                return $"{Name}: ✅ Complete ({TotalMessages:N0} messages)";

            if (MessagesSent == 0)
                return $"{Name}: ⏳ Ready ({TotalMessages:N0} messages)";

            return $"{Name}: 🔄 {MessagesSent:N0}/{TotalMessages:N0} ({ProgressPercentage:F1}%)";
        }

        /// <summary>
        /// Resets progress for retry scenarios
        /// </summary>
        public void Reset()
        {
            MessagesSent = 0;
            IsCompleted = false;
            LastMessageTimestamp = null;
            DiscordChannelId = 0;
        }
    }
}