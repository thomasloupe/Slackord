using Newtonsoft.Json;

namespace Slackord.Classes
{
    /// <summary>
    /// Represents a single import session with persistent file-based storage
    /// </summary>
    public class ImportSession
    {
        public string SessionId { get; set; }
        public string SessionPath { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ChannelProgress> Channels { get; set; } = [];
        public bool IsCompleted => Channels.All(c => c.IsCompleted);

        private static readonly string ImportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Imports");

        /// <summary>
        /// Creates a new import session with timestamped folder
        /// </summary>
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
        /// Gets all incomplete import sessions
        /// </summary>
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
        /// Adds a channel to this session
        /// </summary>
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
            Save(); // Immediately persist
            return progress;
        }

        /// <summary>
        /// Gets the path for a channel's .slackord file
        /// </summary>
        public string GetChannelFilePath(string channelName)
        {
            string sanitizedName = string.Concat(channelName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(SessionPath, $"{sanitizedName}.slackord");
        }

        /// <summary>
        /// Marks a channel as completed
        /// </summary>
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