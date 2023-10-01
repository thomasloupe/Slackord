using MenuApp;
using Newtonsoft.Json.Linq;

namespace Slackord.Classes
{
    public class Deconstruct
    {

        public static DeconstructedMessage DeconstructMessage(JObject slackMessage)
        {
            var deconstructedMessage = new DeconstructedMessage
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
                ThreadTs = slackMessage["thread_ts"]?.ToString(),
                ParentUserId = slackMessage["parent_user_id"]?.ToString(),
                HasRichText = slackMessage["blocks"] != null,
                Attachments = slackMessage["attachments"] != null,
                PreviousMessage = slackMessage["previous"]?["text"]?.ToString(),
                OriginalTimestamp = slackMessage["original_ts"]?.ToString(),
                Subtype = slackMessage["subtype"]?.ToString(),
                EditorId = slackMessage["editor_id"]?.ToString(),
            };
            return deconstructedMessage;
        }
    }

    public class DeconstructedUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string RealName { get; set; }

        public class DeconstructedUsers
        {
            public static Dictionary<string, DeconstructedUser> UsersDict { get; private set; }

            public static void InitializeUsersDict(FileInfo usersFile)
            {
                try
                {
                    var usersDict = new Dictionary<string, DeconstructedUser>();

                    if (usersFile != null)
                    {
                        var jsonContent = File.ReadAllText(usersFile.FullName);
                        var usersArray = JArray.Parse(jsonContent);

                        foreach (JObject userObject in usersArray.Cast<JObject>())
                        {
                            var deconstructedUser = new DeconstructedUser
                            {
                                Id = userObject["id"]?.ToString(),
                                Name = userObject["name"]?.ToString(),
                                DisplayName = userObject["display_name"]?.ToString(),
                                RealName = userObject["real_name"]?.ToString(),
                            };

                            if (deconstructedUser.Id != null)
                            {
                                usersDict[deconstructedUser.Id] = deconstructedUser;
                            }
                        }
                    }

                    UsersDict = usersDict;
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Successfully parsed {usersDict.Count} users.\n"); });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Parsing Users file failed with an exception: {ex.Message}\n"); });
                }
            }

            public static Dictionary<string, DeconstructedUser> ParseUsersFile(FileInfo usersFile)
            {
                try
                {
                    var usersDict = new Dictionary<string, DeconstructedUser>();

                    if (usersFile != null)
                    {
                        var jsonContent = File.ReadAllText(usersFile.FullName);
                        var usersArray = JArray.Parse(jsonContent);

                        foreach (JObject userObject in usersArray.Cast<JObject>())
                        {
                            var deconstructedUser = new DeconstructedUser
                            {
                                Id = userObject["id"]?.ToString(),
                                Name = userObject["name"]?.ToString(),
                                DisplayName = userObject["display_name"]?.ToString(),
                                RealName = userObject["real_name"]?.ToString(),
                            };

                            if (deconstructedUser.Id != null)
                            {
                                usersDict[deconstructedUser.Id] = deconstructedUser;
                            }
                        }
                    }

                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Successfully parsed {usersDict.Count} users.\n"); });
                    return usersDict;
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Parsing Users file failed with an exception: {ex.Message}\n"); });
                    return null;
                }
            }
        }
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
