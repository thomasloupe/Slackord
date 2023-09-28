using Newtonsoft.Json.Linq;

namespace Slackord.Classes
{
    public class Message
    {
        public string Timestamp { get; set; }
        public string UserId { get; set; }
        public string Text { get; set; }
        public List<Attachment> Attachments { get; set; } = new();
        public List<SlackFile> Files { get; set; } = new();
        public BotMessage BotMessage { get; set; }
        public ChannelEvent ChannelEvent { get; set; }
        public List<UserReaction> Reactions { get; set; } = new();
        public List<string> PinnedTo { get; set; } = new();
        public string Content { get; set; }
    }

    public class MessageBuilder
    {
        public static Message BuildMessage(JObject slackMessage)
        {
            var message = new Message
            {
                Timestamp = slackMessage.Value<string>("ts"),
                UserId = slackMessage.Value<string>("user"),
                Text = slackMessage.Value<string>("text"),
                Attachments = AttachmentsParser.ParseAttachments(slackMessage.Value<JArray>("attachments")),
                Files = FilesParser.ParseFiles(slackMessage.Value<JArray>("files")),
                BotMessage = BotsParser.ParseBotMessage(slackMessage),
                ChannelEvent = ChannelEventsParser.ParseChannelEvent(slackMessage),
                Reactions = ReactionsParser.ParseReactions(slackMessage.Value<JArray>("reactions")),
                PinnedTo = PinnedItemsParser.ParsePinnedItems(slackMessage)
            };

            return message;
        }
    }

    public class PinnedItemsParser
    {
        public static List<string> ParsePinnedItems(JObject slackMessage)
        {
            var pinnedTo = new List<string>();
            var slackPinnedTo = slackMessage.Value<JArray>("pinned_to");
            if (slackPinnedTo != null)
            {
                foreach (var channelId in slackPinnedTo)
                {
                    pinnedTo.Add(channelId.Value<string>());
                }
            }

            return pinnedTo;
        }
    }
}