using Slackord.Classes;

namespace Slackord.Classes
{
    public class AppSettings
    {
        private static AppSettings _instance;
        public static AppSettings Instance => _instance ??= new AppSettings();

        // UI Format Settings
        public ApplicationWindow.UserFormatOrder UserFormat { get; set; }
        public string TimestampFormat { get; set; }
        public bool UseThreadsForReplies { get; set; }

        // Discord Bot Settings
        public string BotToken { get; set; }
        public string GuildId { get; set; }

        // Import Settings
        public int RateLimitDelay { get; set; }
        public bool SkipExistingChannels { get; set; }
        public bool ImportAttachments { get; set; }

        // Export Settings
        public bool ExportStatsAfterImport { get; set; }

        // Update Settings
        public bool CheckForUpdatesOnStartup { get; set; }

        // Resume Import Settings
        public bool EnableResumeImport { get; set; }

        // Resume State
        public string LastImportType { get; set; } // "Full" or "Channel"
        public string LastImportChannel { get; set; }
        public string LastSuccessfulMessageTimestamp { get; set; }
        public bool HasPartialImport { get; set; }

        private AppSettings()
        {
            // Load settings from preferences
            LoadFromPreferences();
        }

        public void LoadFromPreferences()
        {
            // UI Format Settings
            string userFormatValue = Preferences.Default.Get("UserFormatValue", ApplicationWindow.UserFormatOrder.DisplayName_User_RealName.ToString());
            UserFormat = Enum.Parse<ApplicationWindow.UserFormatOrder>(userFormatValue);
            TimestampFormat = Preferences.Default.Get("TimestampValue", "12 Hour");

            // Discord Bot Settings
            BotToken = Preferences.Default.Get("SlackordBotToken", string.Empty);
            GuildId = Preferences.Default.Get("SlackordGuildId", string.Empty);

            // Update Settings
            CheckForUpdatesOnStartup = Preferences.Default.Get("CheckForUpdatesOnStartup", true);

            // Resume Import Settings
            EnableResumeImport = Preferences.Default.Get("EnableResumeImport", true);

            // Resume State
            LastImportType = Preferences.Default.Get("LastImportType", string.Empty);
            LastImportChannel = Preferences.Default.Get("LastImportChannel", string.Empty);
            LastSuccessfulMessageTimestamp = Preferences.Default.Get("LastSuccessfulMessageTimestamp", string.Empty);
            HasPartialImport = Preferences.Default.Get("HasPartialImport", false);
        }

        public async Task SaveAsync()
        {
            // UI Format Settings
            Preferences.Default.Set("UserFormatValue", UserFormat.ToString());
            ApplicationWindow.CurrentUserFormatOrder = UserFormat;
            Preferences.Default.Set("TimestampValue", TimestampFormat);

            // Discord Bot Settings
            Preferences.Default.Set("SlackordBotToken", BotToken);
            Preferences.Default.Set("SlackordGuildId", GuildId);

            // Update Settings
            Preferences.Default.Set("CheckForUpdatesOnStartup", CheckForUpdatesOnStartup);

            // Resume State
            Preferences.Default.Set("LastImportType", LastImportType);
            Preferences.Default.Set("LastImportChannel", LastImportChannel);
            Preferences.Default.Set("LastSuccessfulMessageTimestamp", LastSuccessfulMessageTimestamp);
            Preferences.Default.Set("HasPartialImport", HasPartialImport);

            // Update UI if needed
            await UpdateUIAfterSave();
        }

        // This method is marked static to address the CA1822 warning
        private static async Task UpdateUIAfterSave()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Update the user format button text
                await ApplicationWindow.GetUserFormatValue();

                // Update the timestamp button text
                await ApplicationWindow.GetTimeStampValue();
            });
        }

        public void ResetToDefaults()
        {
            // UI Format Settings
            UserFormat = ApplicationWindow.UserFormatOrder.DisplayName_User_RealName;
            TimestampFormat = "12 Hour";
            UseThreadsForReplies = true;

            // Discord Bot Settings
            // We don't reset BotToken for user convenience
            GuildId = string.Empty;

            // Update Settings
            CheckForUpdatesOnStartup = true;
        }

        // Resume-specific methods
        public void SaveResumeState(string importType, string channelName, string messageTimestamp)
        {
            LastImportType = importType;
            LastImportChannel = channelName;
            LastSuccessfulMessageTimestamp = messageTimestamp;
            HasPartialImport = true;

            // Save immediately to preferences
            Preferences.Default.Set("LastImportType", LastImportType);
            Preferences.Default.Set("LastImportChannel", LastImportChannel);
            Preferences.Default.Set("LastSuccessfulMessageTimestamp", LastSuccessfulMessageTimestamp);
            Preferences.Default.Set("HasPartialImport", HasPartialImport);
        }

        public void ClearResumeState()
        {
            LastImportType = string.Empty;
            LastImportChannel = string.Empty;
            LastSuccessfulMessageTimestamp = string.Empty;
            HasPartialImport = false;

            // Save immediately to preferences
            Preferences.Default.Set("LastImportType", LastImportType);
            Preferences.Default.Set("LastImportChannel", LastImportChannel);
            Preferences.Default.Set("LastSuccessfulMessageTimestamp", LastSuccessfulMessageTimestamp);
            Preferences.Default.Set("HasPartialImport", HasPartialImport);
        }
    }
}