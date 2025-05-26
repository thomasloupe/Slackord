using Newtonsoft.Json;

namespace Slackord.Classes
{
    /// <summary>
    /// Represents a single import session with persistent file-based storage
    /// </summary>
    public class ImportSession
    {
        /// <summary>
        /// Gets or sets the unique session identifier based on timestamp
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the file system path where session data is stored
        /// </summary>
        public string SessionPath { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this session was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the list of channel progress trackers for this session
        /// </summary>
        public List<ChannelProgress> Channels { get; set; } = [];

        /// <summary>
        /// Gets whether all channels in this session have completed importing
        /// </summary>
        public bool IsCompleted => Channels.All(c => c.IsCompleted);

        /// <summary>
        /// The base folder path where all import sessions are stored
        /// </summary>
        private static readonly string ImportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imports");

        /// <summary>
        /// Creates a new import session with timestamped folder
        /// </summary>
        /// <returns>A new ImportSession instance with initialized properties</returns>
        public static ImportSession CreateNew()
        {
            var session = new ImportSession
            {
                SessionId = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
                CreatedAt = DateTime.Now
            };

            session.SessionPath = Path.Combine(ImportsFolder, session.SessionId);
            Directory.CreateDirectory(session.SessionPath);

            return session;
        }

        /// <summary>
        /// Loads an existing session from disk
        /// </summary>
        /// <param name="sessionPath">The path to the session directory</param>
        /// <returns>The loaded ImportSession or null if loading fails</returns>
        public static ImportSession Load(string sessionPath)
        {
            string resumeDataPath = Path.Combine(sessionPath, "resume_data.json");
            if (!File.Exists(resumeDataPath))
                return null;

            string json = File.ReadAllText(resumeDataPath);
            var session = JsonConvert.DeserializeObject<ImportSession>(json);
            session.SessionPath = sessionPath;
            return session;
        }

        /// <summary>
        /// Saves the session's resume data to disk
        /// </summary>
        public void Save()
        {
            string resumeDataPath = Path.Combine(SessionPath, "resume_data.json");
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(resumeDataPath, json);
        }

        /// <summary>
        /// Gets all incomplete import sessions from the imports folder
        /// </summary>
        /// <returns>A list of incomplete ImportSession instances ordered by creation date (newest first)</returns>
        public static List<ImportSession> GetIncompleteImports()
        {
            var incompleteSessions = new List<ImportSession>();

            if (!Directory.Exists(ImportsFolder))
                return incompleteSessions;

            foreach (string sessionDir in Directory.GetDirectories(ImportsFolder))
            {
                var session = Load(sessionDir);
                if (session != null && !session.IsCompleted)
                {
                    incompleteSessions.Add(session);
                }
            }

            return [.. incompleteSessions.OrderByDescending(s => s.CreatedAt)];
        }

        /// <summary>
        /// Adds a channel to this session with specified message count
        /// </summary>
        /// <param name="channelName">The name of the channel to add</param>
        /// <param name="totalMessages">The total number of messages in this channel</param>
        /// <returns>The created ChannelProgress instance</returns>
        public ChannelProgress AddChannel(string channelName, int totalMessages)
        {
            var progress = new ChannelProgress
            {
                Name = channelName,
                TotalMessages = totalMessages,
                MessagesSent = 0,
                IsCompleted = false
            };

            Channels.Add(progress);
            Save();
            return progress;
        }

        /// <summary>
        /// Gets the file path for a channel's .slackord data file
        /// </summary>
        /// <param name="channelName">The name of the channel</param>
        /// <returns>The full file path for the channel's .slackord file</returns>
        public string GetChannelFilePath(string channelName)
        {
            string sanitizedName = string.Concat(channelName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(SessionPath, $"{sanitizedName}.slackord");
        }

        /// <summary>
        /// Marks a channel as completed and updates its progress
        /// </summary>
        /// <param name="channelName">The name of the channel to mark as complete</param>
        public void CompleteChannel(string channelName)
        {
            var channel = Channels.FirstOrDefault(c => c.Name == channelName);
            if (channel != null)
            {
                channel.IsCompleted = true;
                channel.MessagesSent = channel.TotalMessages;
                Save();
            }
        }
    }
}