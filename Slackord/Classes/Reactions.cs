using Newtonsoft.Json.Linq;

namespace Slackord.Classes
{
    public class Reaction
    {
        public string Name { get; set; }
        public List<string> UserIds { get; set; } = new List<string>();
    }

    public class UserReaction
    {
        public string UserId { get; set; }
        public List<Reaction> Reactions { get; set; } = new List<Reaction>();
    }

    public class ReactionsParser
    {
        public static List<UserReaction> ParseReactions(JArray slackReactions)
        {
            var userReactionsList = new List<UserReaction>();

            if (slackReactions != null)
            {
                foreach (JObject slackReaction in slackReactions.Cast<JObject>())
                {
                    var reaction = new Reaction
                    {
                        Name = slackReaction.Value<string>("name"),
                    };
                    reaction.UserIds.AddRange(slackReaction["users"].ToObject<List<string>>());

                    foreach (var userId in reaction.UserIds)
                    {
                        var userReaction = userReactionsList.Find(ur => ur.UserId == userId);
                        if (userReaction == null)
                        {
                            userReaction = new UserReaction { UserId = userId };
                            userReactionsList.Add(userReaction);
                        }
                        userReaction.Reactions.Add(reaction);
                    }
                }
            }

            return userReactionsList;
        }
    }
}
