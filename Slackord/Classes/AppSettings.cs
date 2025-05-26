namespace Slackord.Classes
{
    /// <summary>
    /// Manages application settings and preferences with singleton pattern
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Singleton instance field
        /// </summary>
        private static AppSettings _instance;

        /// <summary>
        /// Gets the singleton instance of AppSettings
        /// </summary>
        public static AppSettings Instance => _instance ??= new AppSettings();

        /// <summary>
        /// Gets or sets the user format order for displaying names
        /// </summary>
        public ApplicationWindow.UserFormatOrder UserFormat { get; set; }

        /// <summary>
        /// Gets or sets the timestamp format (12 Hour or 24 Hour)
        /// </summary>
        public string TimestampFormat { get; set; }

        /// <summary>
        /// Gets or sets whether to use threads for reply messages
        /// </summary>
        public bool UseThreadsForReplies { get; set; }

        /// <summary>
        /// Gets or sets the Discord bot authentication token
        /// </summary>
        public string BotToken { get; set; }

        /// <summary>
        /// Gets or sets the Discord guild (server) ID
        /// </summary>
        public string GuildId { get; set; }

        /// <summary>
        /// Gets or sets the Discord logging level (0-5)
        /// </summary>
        public int DiscordLogLevel { get; set; }

        /// <summary>
        /// Gets or sets the rate limit delay between Discord API calls
        /// </summary>
        public int RateLimitDelay { get; set; }

        /// <summary>
        /// Gets or sets whether to skip channels that already exist
        /// </summary>
        public bool SkipExistingChannels { get; set; }

        /// <summary>
        /// Gets or sets whether to import file attachments
        /// </summary>
        public bool ImportAttachments { get; set; }

        /// <summary>
        /// Gets or sets whether to export statistics after import completion
        /// </summary>
        public bool ExportStatsAfterImport { get; set; }

        /// <summary>
        /// Gets or sets whether to check for updates on application startup
        /// </summary>
        public bool CheckForUpdatesOnStartup { get; set; }

        /// <summary>
        /// Gets or sets whether the resume import feature is enabled
        /// </summary>
        public bool EnableResumeImport { get; set; }

        /// <summary>
        /// Gets or sets the type of the last import operation (Full or Channel)
        /// </summary>
        public string LastImportType { get; set; }

        /// <summary>
        /// Gets or sets the name of the last imported channel
        /// </summary>
        public string LastImportChannel { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last successfully processed message
        /// </summary>
        public string LastSuccessfulMessageTimestamp { get; set; }

        /// <summary>
        /// Gets or sets whether there is a partial import that can be resumed
        /// </summary>
        public bool HasPartialImport { get; set; }

        /// <summary>
        /// Initializes a new instance of AppSettings and loads preferences
        /// </summary>
        private AppSettings()
        {
            LoadFromPreferences();
        }

        /// <summary>
        /// Loads all settings from the application preferences store
        /// </summary>
        public void LoadFromPreferences()
        {
            string userFormatValue = Preferences.Default.Get("UserFormatValue", ApplicationWindow.UserFormatOrder.DisplayName_User_RealName.ToString());
            UserFormat = Enum.Parse<ApplicationWindow.UserFormatOrder>(userFormatValue);
            TimestampFormat = Preferences.Default.Get("TimestampValue", "12 Hour");

            BotToken = Preferences.Default.Get("SlackordBotToken", string.Empty);
            GuildId = Preferences.Default.Get("SlackordGuildId", string.Empty);
            DiscordLogLevel = Preferences.Default.Get("DiscordLogLevel", 3);

            CheckForUpdatesOnStartup = Preferences.Default.Get("CheckForUpdatesOnStartup", true);

            EnableResumeImport = Preferences.Default.Get("EnableResumeImport", true);

            LastImportType = Preferences.Default.Get("LastImportType", string.Empty);
            LastImportChannel = Preferences.Default.Get("LastImportChannel", string.Empty);
            LastSuccessfulMessageTimestamp = Preferences.Default.Get("LastSuccessfulMessageTimestamp", string.Empty);
            HasPartialImport = Preferences.Default.Get("HasPartialImport", false);
        }

        /// <summary>
        /// Saves all current settings to the application preferences store and updates UI
        /// </summary>
        public async Task SaveAsync()
        {
            Preferences.Default.Set("UserFormatValue", UserFormat.ToString());
            ApplicationWindow.CurrentUserFormatOrder = UserFormat;
            Preferences.Default.Set("TimestampValue", TimestampFormat);

            Preferences.Default.Set("SlackordBotToken", BotToken);
            Preferences.Default.Set("SlackordGuildId", GuildId);
            Preferences.Default.Set("DiscordLogLevel", DiscordLogLevel);

            Preferences.Default.Set("CheckForUpdatesOnStartup", CheckForUpdatesOnStartup);

            Preferences.Default.Set("LastImportType", LastImportType);
            Preferences.Default.Set("LastImportChannel", LastImportChannel);
            Preferences.Default.Set("LastSuccessfulMessageTimestamp", LastSuccessfulMessageTimestamp);
            Preferences.Default.Set("HasPartialImport", HasPartialImport);

            await UpdateUIAfterSave();
        }

        /// <summary>
        /// Updates the UI elements after saving settings
        /// </summary>
        private static async Task UpdateUIAfterSave()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ApplicationWindow.GetUserFormatValue();
                await ApplicationWindow.GetTimeStampValue();
            });
        }

        /// <summary>
        /// Resets all settings to their default values
        /// </summary>
        public void ResetToDefaults()
        {
            UserFormat = ApplicationWindow.UserFormatOrder.DisplayName_User_RealName;
            TimestampFormat = "12 Hour";
            UseThreadsForReplies = true;

            GuildId = string.Empty;
            DiscordLogLevel = 3;

            CheckForUpdatesOnStartup = true;
        }

        /// <summary>
        /// Saves the current state for resume functionality
        /// </summary>
        /// <param name="importType">The type of import (Full or Channel)</param>
        /// <param name="channelName">The name of the channel being processed</param>
        /// <param name="messageTimestamp">The timestamp of the last processed message</param>
        public void SaveResumeState(string importType, string channelName, string messageTimestamp)
        {
            LastImportType = importType;
            LastImportChannel = channelName;
            LastSuccessfulMessageTimestamp = messageTimestamp;
            HasPartialImport = true;

            Preferences.Default.Set("LastImportType", LastImportType);
            Preferences.Default.Set("LastImportChannel", LastImportChannel);
            Preferences.Default.Set("LastSuccessfulMessageTimestamp", LastSuccessfulMessageTimestamp);
            Preferences.Default.Set("HasPartialImport", HasPartialImport);
        }

        /// <summary>
        /// Clears the saved resume state information
        /// </summary>
        public void ClearResumeState()
        {
            LastImportType = string.Empty;
            LastImportChannel = string.Empty;
            LastSuccessfulMessageTimestamp = string.Empty;
            HasPartialImport = false;

            Preferences.Default.Set("LastImportType", LastImportType);
            Preferences.Default.Set("LastImportChannel", LastImportChannel);
            Preferences.Default.Set("LastSuccessfulMessageTimestamp", LastSuccessfulMessageTimestamp);
            Preferences.Default.Set("HasPartialImport", HasPartialImport);
        }

        /// <summary>
        /// Gets a user-friendly description of the Discord log level
        /// </summary>
        /// <returns>A descriptive string explaining the current log level</returns>
        public string GetLogLevelDescription()
        {
            return DiscordLogLevel switch
            {
                0 => "Critical - Only the most severe errors",
                1 => "Error - Recoverable errors and issues",
                2 => "Warning - Non-critical issues and warnings",
                3 => "Info - General operational information",
                4 => "Debug - Detailed debugging information",
                5 => "Verbose - All possible log messages",
                _ => "Info - General operational information"
            };
        }
    }
}