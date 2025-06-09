using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Slackord.Classes.DeconstructedUser;

namespace Slackord.Classes
{
    /// <summary>
    /// Handles the deconstruction of Slack messages into structured data
    /// </summary>
    public class Deconstruct
    {
        /// <summary>
        /// Dictionary to track thread information during deconstruction
        /// </summary>
        private static readonly Dictionary<string, ThreadInfo> threadDictionary = [];

        /// <summary>
        /// Gets a read-only view of the thread dictionary
        /// </summary>
        public static IReadOnlyDictionary<string, ThreadInfo> ThreadDictionary
        {
            get { return threadDictionary; }
        }

        /// <summary>
        /// Deconstructs a Slack message JSON object into a structured DeconstructedMessage
        /// </summary>
        /// <param name="slackMessage">The Slack message JSON object to deconstruct</param>
        /// <returns>A DeconstructedMessage containing the parsed data</returns>
        public static DeconstructedMessage DeconstructMessage(JObject slackMessage)
        {
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
                deconstructedMessage.Reactions = currentValue?.ToObject<List<Reaction>>() ?? [];

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

                if (threadTs == timestamp)
                {
                    if (!threadDictionary.ContainsKey(threadTs))
                    {
                        threadDictionary[threadTs] = new ThreadInfo();
                    }
                }

                currentProperty = "files";
                if (slackMessage[currentProperty] is JArray files)
                {
                    foreach (var file in files)
                    {
                        currentProperty = "mode";
                        currentValue = file[currentProperty];
                        if (currentValue?.ToString() == "hidden_by_limit")
                        {
                            deconstructedMessage.FileURLs.Add("File is hidden by Slack due to limits.");
                            deconstructedMessage.IsFileDownloadable.Add(false);
                            ImportJson.TotalHiddenFileCount++;
                            continue;
                        }

                        // Prefer Slackdump local path if available
                        currentProperty = "local_path";
                        currentValue = file[currentProperty];
                        string localPath = currentValue?.ToString();
                        if (!string.IsNullOrEmpty(localPath))
                        {
                            deconstructedMessage.FileURLs.Add(localPath);
                            deconstructedMessage.IsFileDownloadable.Add(true);
                            continue;
                        }

                        currentProperty = "url_private_download";
                        currentValue = file[currentProperty];
                        var fileUrl = currentValue?.ToString();

                        if (string.IsNullOrEmpty(fileUrl))
                        {
                            string logMessage = $"Empty or null file URL found. Channel: {slackMessage.Root}";
                            Logger.Log(logMessage);
                        }
                        else
                        {
                            deconstructedMessage.FileURLs.Add(fileUrl);
                            deconstructedMessage.IsFileDownloadable.Add(true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string logMessage = $"DeconstructMessage(): Error processing property '{currentProperty}' with value '{currentValue}' in JSON. {ex.Message}";
                Logger.Log(logMessage);
                throw new Exception($"DeconstructMessage(): Error processing property '{currentProperty}' with value '{currentValue}' in JSON", ex);
            }

            return deconstructedMessage;
        }

        /// <summary>
        /// Determines the thread type of a Slack message
        /// </summary>
        /// <param name="slackMessage">The Slack message JSON object</param>
        /// <returns>The thread type (None, Parent, or Reply)</returns>
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

    /// <summary>
    /// Represents a deconstructed Slack user with profile information
    /// </summary>
    public class DeconstructedUser
    {
        /// <summary>
        /// Gets or sets the user ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the username
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user profile information
        /// </summary>
        [JsonProperty("profile")]
        public UserProfile Profile { get; set; }

        /// <summary>
        /// Represents a user's profile information from Slack
        /// </summary>
        public class UserProfile
        {
            /// <summary>
            /// Gets or sets the display name
            /// </summary>
            [JsonProperty("display_name")]
            public string DisplayName { get; set; }

            /// <summary>
            /// Gets or sets the real name
            /// </summary>
            [JsonProperty("real_name")]
            public string RealName { get; set; }

            /// <summary>
            /// Gets or sets the avatar image URL
            /// </summary>
            [JsonProperty("image_192")]
            public string Avatar { get; set; }
        }
    }

    /// <summary>
    /// Manages the parsing and storage of Slack users data
    /// </summary>
    public class DeconstructedUsers
    {
        /// <summary>
        /// Gets the dictionary of parsed users indexed by user ID
        /// </summary>
        public static Dictionary<string, DeconstructedUser> UsersDict { get; private set; }

        /// <summary>
        /// Initializes the users dictionary from a users.json file
        /// </summary>
        /// <param name="usersFile">The users.json file to parse</param>
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
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"InitializeUsersDict(): Parsing Users file failed with an exception: {ex.Message}\n"); });
            }
        }

        /// <summary>
        /// Parses a users.json file and returns a dictionary of users
        /// </summary>
        /// <param name="usersFile">The users.json file to parse</param>
        /// <returns>Dictionary of users indexed by user ID, or null if parsing fails</returns>
        public static Dictionary<string, DeconstructedUser> ParseUsersFile(FileInfo usersFile)
        {
            try
            {
                Dictionary<string, DeconstructedUser> usersDict = [];

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
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ParseUsersFile(): Parsing Users file failed with an exception: {ex.Message}\n"); });
                return null;
            }
        }
    }

    /// <summary>
    /// Defines the type of thread relationship for a message
    /// </summary>
    public enum ThreadType
    {
        /// <summary>
        /// Message is not part of a thread
        /// </summary>
        None,
        /// <summary>
        /// Message is the parent/start of a thread
        /// </summary>
        Parent,
        /// <summary>
        /// Message is a reply within a thread
        /// </summary>
        Reply
    }

    /// <summary>
    /// Contains information about a thread for Discord mapping
    /// </summary>
    public class ThreadInfo
    {
        /// <summary>
        /// Gets or sets the Discord message ID for the thread
        /// </summary>
        public string DiscordMessageId { get; set; }
    }

    /// <summary>
    /// Represents a deconstructed Slack message with all its components
    /// </summary>
    public class DeconstructedMessage
    {
        /// <summary>
        /// Gets or sets the original Slack message JSON string
        /// </summary>
        public string OriginalSlackMessageJson { get; set; }

        /// <summary>
        /// Gets or sets the message type
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Gets or sets the channel identifier
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who sent the message
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Gets or sets the message text content
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the message timestamp
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Gets or sets whether the message is starred
        /// </summary>
        public bool IsStarred { get; set; }

        /// <summary>
        /// Gets or sets whether the message is pinned
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// Gets or sets whether the message was sent by a bot
        /// </summary>
        public bool IsBot { get; set; }

        /// <summary>
        /// Gets or sets the list of reactions on the message
        /// </summary>
        public List<Reaction> Reactions { get; set; }

        /// <summary>
        /// Gets or sets the thread timestamp
        /// </summary>
        public string ThreadTs { get; set; }

        /// <summary>
        /// Gets or sets the thread type (None, Parent, Reply)
        /// </summary>
        public ThreadType ThreadType { get; set; }

        /// <summary>
        /// Gets or sets the parent thread timestamp
        /// </summary>
        public string ParentThreadTs { get; set; }

        /// <summary>
        /// Gets or sets the parent user ID for thread replies
        /// </summary>
        public string ParentUserId { get; set; }

        /// <summary>
        /// Gets or sets whether the message has rich text formatting
        /// </summary>
        public bool HasRichText { get; set; }

        /// <summary>
        /// Gets or sets whether the message has attachments
        /// </summary>
        public bool Attachments { get; set; }

        /// <summary>
        /// Gets or sets the previous message text (for edits)
        /// </summary>
        public string PreviousMessage { get; set; }

        /// <summary>
        /// Gets or sets the original timestamp (for edits)
        /// </summary>
        public string OriginalTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the message subtype
        /// </summary>
        public string Subtype { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who edited the message
        /// </summary>
        public string EditorId { get; set; }

        /// <summary>
        /// Gets or sets the list of file URLs attached to the message
        /// </summary>
        public List<string> FileURLs { get; set; } = [];

        /// <summary>
        /// Gets or sets the list indicating which files are downloadable
        /// </summary>
        public List<bool> IsFileDownloadable { get; set; } = [];
    }

    /// <summary>
    /// Represents a reaction emoji on a Slack message
    /// </summary>
    public class Reaction
    {
        /// <summary>
        /// Gets or sets the name of the reaction emoji
        /// </summary>
        public string Name { get; set; }
    }
}