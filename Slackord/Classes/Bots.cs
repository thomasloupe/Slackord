namespace Slackord.Classes
{
    using Newtonsoft.Json.Linq;

    public class BotMessage
    {
        public bool IsBot { get; set; }
        public string BotId { get; set; }
    }

    public class BotsParser
    {
        public static BotMessage ParseBotMessage(JObject slackMessage)
        {
            var botMessage = new BotMessage();

            var subtype = slackMessage.Value<string>("subtype");
            botMessage.IsBot = subtype == "bot_message";
            botMessage.BotId = slackMessage.Value<string>("bot_id");

            return botMessage;
        }
    }
}
