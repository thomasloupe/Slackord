using MenuApp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Slackord.Classes.DeconstructedUser;

namespace Slackord.Classes
{
    public class Deconstruct
    {

        public static Dictionary<string, ThreadInfo> ThreadDictionary = new();

        public static DeconstructedMessage DeconstructMessage(JObject slackMessage)
        {
            string threadTs = slackMessage["thread_ts"]?.ToString();
            string timestamp = slackMessage["ts"]?.ToString();

            DeconstructedMessage deconstructedMessage = new()
            {
                MessageType = slackMessage["type"]?.ToString(),
                Channel = slackMessage["channel"]?.ToString(),
                User = slackMessage["user"]?.ToString(),
                Text = slackMessage["text"]?.ToString(),
                Timestamp = slackMessage["ts"]?.ToString(),
                IsStarred = slackMessage["is_starred"]?.ToObject<bool>() ?? false,
                IsPinned = slackMessage["pinned_to"] != null,
                IsBot = slackMessage["subtype"]?.ToString() == "bot_message",
                Reactions = slackMessage["reactions"]?.ToObject<List<Reaction>>() ?? new List<Reaction>(),
                ThreadTs = threadTs,
                ParentUserId = slackMessage["parent_user_id"]?.ToString(),
                HasRichText = slackMessage["blocks"] != null,
                Attachments = slackMessage["attachments"] != null,
                PreviousMessage = slackMessage["previous"]?["text"]?.ToString(),
                OriginalTimestamp = slackMessage["original_ts"]?.ToString(),
                Subtype = slackMessage["subtype"]?.ToString(),
                EditorId = slackMessage["editor_id"]?.ToString(),
                ThreadType = DetermineThreadType(slackMessage),
                ParentThreadTs = slackMessage["thread_ts"]?.ToString(),
            };

            if (threadTs == timestamp)  // This is a thread parent message.
            {
                if (!ThreadDictionary.ContainsKey(threadTs))
                {
                    ThreadDictionary[threadTs] = new ThreadInfo();
                }
            }

            return deconstructedMessage;
        }

        private static ThreadType DetermineThreadType(JObject slackMessage)
        {
            string threadTs = slackMessage["thread_ts"]?.ToString();
            string timestamp = slackMessage["ts"]?.ToString();

            return threadTs == null ? ThreadType.None : threadTs == timestamp ? ThreadType.Parent : ThreadType.Reply;
        }
    }

    public class DeconstructedUser
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("profile")]
        public UserProfile Profile { get; set; }

        public class UserProfile
        {
            [JsonProperty("display_name")]
            public string DisplayName { get; set; }

            [JsonProperty("real_name")]
            public string RealName { get; set; }
        }
    }

    public class DeconstructedUsers
    {
        public static Dictionary<string, DeconstructedUser> UsersDict { get; private set; }

        public static void InitializeUsersDict(FileInfo usersFile)
        {
            try
            {
                Dictionary<string, DeconstructedUser> usersDict = new();

                if (usersFile != null)
                {
                    string jsonContent = File.ReadAllText(usersFile.FullName);
                    JArray usersArray = JArray.Parse(jsonContent);

                    foreach (JObject userObject in usersArray.Cast<JObject>())
                    {
                        DeconstructedUser deconstructedUser = userObject.ToObject<DeconstructedUser>();

                        if (deconstructedUser.Id == null)
                        {
                            continue;
                        }
                        usersDict[deconstructedUser.Id] = deconstructedUser;
                    }
                }

                UsersDict = usersDict;
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Successfully parsed {usersDict.Count} users.\n"); });
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Parsing Users file failed with an exception: {ex.Message}\n"); });
            }
        }

        public static Dictionary<string, DeconstructedUser> ParseUsersFile(FileInfo usersFile)
        {
            try
            {
                Dictionary<string, DeconstructedUser> usersDict = new();

                if (usersFile != null)
                {
                    string jsonContent = File.ReadAllText(usersFile.FullName);
                    JArray usersArray = JArray.Parse(jsonContent);

                    foreach (JObject userObject in usersArray.Cast<JObject>())
                    {
                        DeconstructedUser deconstructedUser = new()
                        {
                            Id = userObject["id"]?.ToString(),
                            Name = userObject["name"]?.ToString(),
                            Profile = userObject["profile"]?.ToObject<UserProfile>()
                        };

                        if (deconstructedUser.Id != null)
                        {
                            usersDict[deconstructedUser.Id] = deconstructedUser;
                        }
                        // If user count is 0, then the users.json file is empty or not found.
                        _ = usersDict.Count == 0
                            ? Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"No users found in users.json file.\n"); })
                            : Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Successfully parsed {usersDict.Count} users.\n"); });
                    }
                }
                return usersDict;
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Parsing Users file failed with an exception: {ex.Message}\n"); });
                return null;
            }
        }
    }

    public class ThreadInfo
    {
        public ThreadInfo(string discordMessageId)
        {
            DiscordMessageId = discordMessageId;
        }

        public string DiscordMessageId { get; set; }
    }

    public class DeconstructedMessage
    {
        public string MessageType { get; set; }
        public string Channel { get; set; }
        public string User { get; set; }
        public string Text { get; set; }
        public string Timestamp { get; set; }
        public bool IsStarred { get; set; }
        public bool IsPinned { get; set; }
        public bool IsBot { get; set; }
        public List<Reaction> Reactions { get; set; }
        public string ThreadTs { get; set; }
        public ThreadType ThreadType { get; set; }
        public string ParentThreadTs { get; set; }
        public string ParentUserId { get; set; }
        public bool HasRichText { get; set; }
        public bool Attachments { get; set; }
        public string PreviousMessage { get; set; }
        public string OriginalTimestamp { get; set; }
        public string Subtype { get; set; }
        public string EditorId { get; set; }
    }

    public class Reaction
    {
        public string Name { get; set; }
    }
}
