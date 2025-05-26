namespace Slackord.Classes
{
    /// <summary>
    /// Represents a Slack channel with its associated messages during the import process
    /// </summary>
    public class Channel
    {
        /// <summary>
        /// Gets or sets the name of the Slack channel
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Discord channel ID after channel creation
        /// </summary>
        public ulong DiscordChannelId { get; set; }

        /// <summary>
        /// Gets or sets the channel description or topic
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the list of deconstructed Slack messages
        /// </summary>
        public List<DeconstructedMessage> DeconstructedMessagesList { get; set; } = [];

        /// <summary>
        /// Gets or sets the list of reconstructed messages ready for Discord posting
        /// </summary>
        public List<ReconstructedMessage> ReconstructedMessagesList { get; set; } = [];
    }
}