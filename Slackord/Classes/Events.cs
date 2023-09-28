namespace Slackord.Classes
{
    using Newtonsoft.Json.Linq;

    public class ChannelEvent
    {
        public bool IsJoin { get; set; }
        public string UserId { get; set; }
    }

    public class ChannelEventsParser
    {
        public static ChannelEvent ParseChannelEvent(JObject slackMessage)
        {
            var channelEvent = new ChannelEvent();

            var subtype = slackMessage.Value<string>("subtype");
            channelEvent.IsJoin = subtype == "channel_join";
            // Assuming the user ID is in a field called "user", adjust as needed
            channelEvent.UserId = slackMessage.Value<string>("user");

            return channelEvent;
        }
    }
}
