namespace Slackord.Classes
{
    public class Channel
    {
        public string Name { get; set; }
        public ulong DiscordChannelId { get; set; }
        public string Description { get; set; }
        public List<DeconstructedMessage> DeconstructedMessagesList { get; set; } = [];
        public List<ReconstructedMessage> ReconstructedMessagesList { get; set; } = [];
    }
}
