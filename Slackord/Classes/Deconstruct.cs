using MenuApp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Slackord.Classes.DeconstructedUser;

namespace Slackord.Classes
{
    public class Deconstruct
    {

        private static readonly Dictionary<string, ThreadInfo> threadDictionary = new();

        public static IReadOnlyDictionary<string, ThreadInfo> ThreadDictionary
        {
            get { return threadDictionary; }
        }

        public static DeconstructedMessage DeconstructMessage(JObject slackMessage)
        {
            // This is original unmodified JSON from Slack. We keep this in case we run into issues with the deconstructed message for review.
            DeconstructedMessage deconstructedMessage = new()
            {
                OriginalSlackMessageJson = slackMessage.ToString()
            };

            string currentProperty = "";
            JToken currentValue = null;

            try
            {
                currentProperty = "thread_ts";
                currentValue = slackMessage[currentProperty];
                string threadTs = currentValue?.ToString();

                currentProperty = "ts";
                currentValue = slackMessage[currentProperty];
                string timestamp = currentValue?.ToString();

                currentProperty = "type";
                currentValue = slackMessage[currentProperty];
                deconstructedMessage.MessageType = currentValue?.ToString();

                currentProperty = "channel";
                currentValue = slackMessage[currentProperty];
                deconstructedMessage.Channel = currentValue?.ToString();

                currentProperty = "user";
                currentValue = slackMessage[currentProperty];
                deconstructedMessage.User = currentValue?.ToString();

                currentProperty = "text";
                currentValue = slackMessage[currentProperty];
                deconstructedMessage.Text = currentValue?.ToString();

                currentProperty = "ts";
                currentValue = slackMessage[currentProperty];
                deconstructedMessage.Timestamp = currentValue?.ToString();

                currentProperty = "is_starred";
                currentValue = slackMessage[currentProperty];
                deconstructedMessage.IsStarred = currentValue?.ToObject<bool>() ?? false;

                currentProperty = "pinned_to";
                deconstructedMessage.IsPinned = slackMessage[currentProperty] != null;

                currentProperty = "subtype";
                currentValue = slackMessage[currentProperty];
                deconstructedMessage.IsBot = currentValue?.ToString() == "bot_message";

                currentProperty = "reactions";
                currentValue = slackMessage[currentProperty];
                deconstructedMessage.Reactions = currentValue?.ToObject<List<Reaction>>() ?? new List<Reaction>();

                deconstructedMessage.ThreadTs = threadTs;

                currentProperty = "parent_user_id";
                currentValue = slackMessage[currentProperty];
                deconstructedMessage.ParentUserId = currentValue?.ToString();

                currentProperty = "blocks";
                deconstructedMessage.HasRichText = slackMessage[currentProperty] != null;

                currentProperty = "attachments";
                deconstructedMessage.Attachments = slackMessage[currentProperty] != null;

                currentProperty = "previous.text";
                currentValue = slackMessage["previous"]?["text"];
                deconstructedMessage.PreviousMessage = currentValue?.ToString();

                currentProperty = "original_ts";
                currentValue = slackMessage[currentProperty];
                deconstructedMessage.OriginalTimestamp = currentValue?.ToString();

                currentProperty = "subtype";
                currentValue = slackMessage[currentProperty];
                deconstructedMessage.Subtype = currentValue?.ToString();

                currentProperty = "editor_id";
                currentValue = slackMessage[currentProperty];
                deconstructedMessage.EditorId = currentValue?.ToString();

                deconstructedMessage.ThreadType = DetermineThreadType(slackMessage);

                deconstructedMessage.ParentThreadTs = threadTs;

                if (threadTs == timestamp)  // This is a thread parent message.
                {
                    if (!ThreadDictionary.ContainsKey(threadTs))
                    {
                        threadDictionary[threadTs] = new ThreadInfo();
                    }
                }

                currentProperty = "files";
                if (slackMessage[currentProperty] is JArray files)
                {
                    foreach (var file in files)
                    {
                        currentProperty = "url_private_download";
                        currentValue = file[currentProperty];
                        var fileUrl = currentValue?.ToString();

                        if (string.IsNullOrEmpty(fileUrl))
                        {
                            // Log empty or null file URLs.
                            _ = Application.Current.Dispatcher.Dispatch(() =>
                            {
                                ApplicationWindow.WriteToDebugWindow($"A file was found that Slack has hidden due to limits. Check the log for more information.\n");
                                string logMessage = $"Empty or null file URL found. Channel: {slackMessage.Root}";
                                Logger.Log(logMessage);
                            });
                        }
                        else
                        {
                            deconstructedMessage.FileURLs.Add(fileUrl);
                            deconstructedMessage.IsFileDownloadable.Add(true);
                        }

                        currentProperty = "mode";
                        currentValue = file[currentProperty];
                        if (currentValue?.ToString() == "hidden_by_limit")
                        {
                            deconstructedMessage.FileURLs.Add("File is hidden by Slack due to limits.");
                            deconstructedMessage.IsFileDownloadable.Add(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string logMessage = $"Error processing property '{currentProperty}' with value '{currentValue}' in JSON. {ex.Message}";
                Logger.Log(logMessage);
                throw new Exception($"Error processing property '{currentProperty}' with value '{currentValue}' in JSON", ex);
            }

            return deconstructedMessage;
        }

        private static ThreadType DetermineThreadType(JObject slackMessage)
        {
            string threadTs = slackMessage["thread_ts"]?.ToString();
            string timestamp = slackMessage["ts"]?.ToString();

            if (threadTs == null)
            {
                return ThreadType.None;
            }
            else if (threadTs == timestamp)
            {
                return ThreadType.Parent;
            }
            else
            {
                return ThreadType.Reply;
            }
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

            [JsonProperty("image_192")]
            public string Avatar { get; set; }
        }
    }

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
                    string jsonContent = File.ReadAllText(usersFile.FullName);
                    JArray usersArray = JArray.Parse(jsonContent);

                    foreach (JObject userObject in usersArray.Cast<JObject>())
                    {
                        DeconstructedUser deconstructedUser = userObject.ToObject<DeconstructedUser>();

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
                            Profile = userObject["profile"]?.ToObject<UserProfile>(),
                        };

                        if (deconstructedUser.Id != null)
                        {
                            usersDict[deconstructedUser.Id] = deconstructedUser;
                        }
                    }
                }
                if (usersDict.Count == 0)
                {
                    // If user count is 0, then the users.json file is empty or not found.
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"No users found in users.json file.\n"); });
                }
                else
                {
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Successfully parsed {usersDict.Count} users.\n"); });
                }
                return usersDict;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Parsing Users file failed with an exception: {ex.Message}\n"); });
                return null;
            }
        }
    }

    public enum ThreadType
    {
        None,
        Parent,
        Reply
    }

    public class ThreadInfo
    {
        public string DiscordMessageId { get; set; }
    }

    public class DeconstructedMessage
    {
        public string OriginalSlackMessageJson { get; set; }
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
        public List<string> FileURLs { get; set; } = new List<string>();
        public List<bool> IsFileDownloadable { get; set; } = new List<bool>();
    }

    public class Reaction
    {
        public string Name { get; set; }
    }
}