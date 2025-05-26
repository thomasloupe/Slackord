using Newtonsoft.Json;

namespace Slackord.Classes
{
    /// <summary>
    /// Tracks progress for a single channel during import process
    /// </summary>
    public class ChannelProgress
    {
        /// <summary>
        /// Gets or sets the original Slack channel name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Discord channel ID once created (0 if not created yet)
        /// </summary>
        public ulong DiscordChannelId { get; set; }

        /// <summary>
        /// Gets or sets the total number of messages to import for this channel
        /// </summary>
        public int TotalMessages { get; set; }

        /// <summary>
        /// Gets or sets the number of messages successfully sent to Discord
        /// </summary>
        public int MessagesSent { get; set; }

        /// <summary>
        /// Gets or sets whether this channel has completed importing
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last successfully sent message
        /// </summary>
        public string LastMessageTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the Discord channel description/topic
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets whether the .slackord file has been created for this channel
        /// </summary>
        public bool FileCreated { get; set; }

        /// <summary>
        /// Gets the progress percentage (0-100) for this channel
        /// </summary>
        [JsonIgnore]
        public double ProgressPercentage => TotalMessages > 0 ? (double)MessagesSent / TotalMessages * 100 : 0;

        /// <summary>
        /// Gets whether this channel needs to be created on Discord
        /// </summary>
        [JsonIgnore]
        public bool NeedsDiscordChannel => DiscordChannelId == 0;

        /// <summary>
        /// Gets the number of messages remaining to send
        /// </summary>
        [JsonIgnore]
        public int MessagesRemaining => Math.Max(0, TotalMessages - MessagesSent);

        /// <summary>
        /// Updates the progress when a message is successfully sent
        /// </summary>
        /// <param name="messageTimestamp">The timestamp of the message that was sent</param>
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
        /// <param name="channelId">The Discord channel ID</param>
        public void SetDiscordChannelId(ulong channelId)
        {
            DiscordChannelId = channelId;
        }

        /// <summary>
        /// Gets a display string for UI showing progress
        /// </summary>
        /// <returns>A formatted string showing channel progress</returns>
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