namespace Slackord.Classes
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    public class Parser
    {
        public static List<Thread> Convert(ImportJson importJson)
        {
            var threadsManager = new ThreadsManager();

            foreach (var channel in ImportJson.Channels)
            {
                System.Collections.IList list = channel.Messages;
                for (int i = 0; i < list.Count; i++)
                {
                    JObject slackMessage = (JObject)list[i];
                    var discordMessage = MessageBuilder.BuildMessage(slackMessage);

                    var threadTs = slackMessage.Value<string>("thread_ts");
                    threadsManager.AddMessage(discordMessage, threadTs);
                }
            }

            return threadsManager.GetAllThreads();
        }
    }
}
