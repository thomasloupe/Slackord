using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slackord.Classes
{
    public class Reconstruct
    {
        public static string ConvertTextContent(string slackText)
        {
            // For simplicity, assume both platforms use similar markdown formatting
            // If there are specific differences, they would be handled here
            return slackText;
        }

        public static EmbedBuilder ConvertAttachments(List<Attachment> slackAttachments)
        {
            var discordEmbedBuilder = new EmbedBuilder();
            foreach (var attachment in slackAttachments)
            {
                // Assume each Slack attachment translates to a field in a Discord embed
                discordEmbedBuilder.AddField(attachment.Title, attachment.Text);
            }
            return discordEmbedBuilder;
        }

        public static List<string> ConvertReactions(List<UserReaction> slackReactions)
        {
            var discordReactions = new List<string>();
            foreach (var userReaction in slackReactions)
            {
                foreach (var reaction in userReaction.Reactions)
                {
                    // Assume emoji names are the same on both platforms
                    discordReactions.Add(reaction.Name);
                }
            }
            return discordReactions;
        }

        public static async Task ConvertThreads(List<Thread> slackThreads, SocketTextChannel channel)
        {
            foreach (var thread in slackThreads)
            {
                if (thread.Messages.Count > 0)
                {
                    // Send the parent message to the channel
                    await channel.SendMessageAsync(thread.Messages[0].Text).ConfigureAwait(false);

                    // Fetch the latest message from the channel which should be the parent message
                    var threadMessages = await channel.GetMessagesAsync(1).FlattenAsync();
                    var parentMessage = threadMessages.First();

                    // Create a thread from the parent message
                    var threadName = GetThreadName(thread.Messages[0].Text);
                    var discordThread = await channel.CreateThreadAsync(threadName, ThreadType.PublicThread, ThreadArchiveDuration.OneDay, parentMessage);

                    // Send replies to the thread
                    for (int i = 1; i < thread.Messages.Count; i++)
                    {
                        await discordThread.SendMessageAsync(thread.Messages[i].Text);
                    }
                }
            }
        }

        private static string GetThreadName(string text)
        {
            // Get the first 20 characters of the text as the thread name, or the entire text if it's shorter than 20 characters
            return text.Length <= 20 ? text : text[..20];
        }
    }
}
