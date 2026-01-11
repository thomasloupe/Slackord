using Discord;
using Slackord.Classes;

namespace Slackord.Pages
{
    /// <summary>
    /// Options page for configuring application settings including bot token, timestamps, and cleanup behavior.
    /// </summary>
    public partial class OptionsPage : ContentPage
    {
        private bool _isPasswordVisible = false;

        /// <summary>
        /// Defines the cleanup behavior options for post-import cleanup
        /// </summary>
        public enum CleanupBehavior
        {
            /// <summary>
            /// Prompt the user before cleaning up files
            /// </summary>
            Prompt = 0,

            /// <summary>
            /// Automatically clean up files without prompting
            /// </summary>
            Automatically = 1,

            /// <summary>
            /// Never clean up files after import
            /// </summary>
            Never = 2
        }

        /// <summary>
        /// Initializes the Options page and loads settings
        /// </summary>
        public OptionsPage()
        {
            InitializeComponent();
            LoadSettings();
            UpdateBotTokenEditability();
        }

        /// <summary>
        /// Loads saved settings from preferences and updates UI controls
        /// </summary>
        private void LoadSettings()
        {
            string userFormatValue = Preferences.Default.Get("UserFormatValue", ApplicationWindow.CurrentUserFormatOrder.ToString());
            int userFormatIndex = (int)Enum.Parse<ApplicationWindow.UserFormatOrder>(userFormatValue);
            UserFormatPicker.SelectedIndex = userFormatIndex;

            string timestampValue = Preferences.Default.Get("TimestampValue", "12 Hour");
            TimestampFormatPicker.SelectedIndex = timestampValue switch
            {
                "12 Hour" => 0,
                "24 Hour" => 1,
                "Remove Timestamps" => 2,
                _ => 0
            };

            BotTokenEntry.Text = Preferences.Default.Get("SlackordBotToken", string.Empty);

            int logLevel = Preferences.Default.Get("DiscordLogLevel", 3);
            LogLevelPicker.SelectedIndex = logLevel;

            bool checkForUpdates = Preferences.Default.Get("CheckForUpdatesOnStartup", true);
            CheckUpdatesSwitch.IsToggled = checkForUpdates;

            int cleanupBehavior = Preferences.Default.Get("CleanupAfterImport", (int)CleanupBehavior.Prompt);
            CleanupAfterImportPicker.SelectedIndex = cleanupBehavior;
        }

        /// <summary>
        /// Called when the bot connection state changes, updates token field editability
        /// </summary>
        public void OnBotConnectionStateChanged()
        {
            UpdateBotTokenEditability();
        }

        /// <summary>
        /// Called when the page is appearing, updates token field editability
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateBotTokenEditability();
        }

        /// <summary>
        /// Toggles password visibility of the bot token entry
        /// </summary>
        private void ToggleTokenVisibility_Clicked(object sender, EventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            BotTokenEntry.IsPassword = !_isPasswordVisible;
            ((Button)sender).Text = _isPasswordVisible ? "Hide" : "Reveal";
        }

        /// <summary>
        /// Saves the settings entered on the Options page
        /// </summary>
        private async void SaveButton_Clicked(object sender, EventArgs e)
        {
            bool tokenChanged = false;
            string currentToken = Preferences.Default.Get("SlackordBotToken", string.Empty);

            if (UserFormatPicker.SelectedIndex >= 0)
            {
                var selectedUserFormat = (ApplicationWindow.UserFormatOrder)UserFormatPicker.SelectedIndex;
                Preferences.Default.Set("UserFormatValue", selectedUserFormat.ToString());
                ApplicationWindow.CurrentUserFormatOrder = selectedUserFormat;
            }

            if (TimestampFormatPicker.SelectedIndex >= 0)
            {
                string timestampValue = TimestampFormatPicker.SelectedIndex switch
                {
                    0 => "12 Hour",
                    1 => "24 Hour",
                    2 => "Remove Timestamps",
                    _ => "12 Hour"
                };
                Preferences.Default.Set("TimestampValue", timestampValue);
            }

            if (BotTokenEntry.Text?.Trim() != currentToken)
            {
                tokenChanged = true;
                if (!string.IsNullOrEmpty(BotTokenEntry.Text))
                {
                    string token = BotTokenEntry.Text.Trim();
                    if (ValidateBotToken(token))
                    {
                        Preferences.Default.Set("SlackordBotToken", token);
                    }
                    else
                    {
                        await DisplayAlertAsync("Warning", "The bot token you entered appears to be invalid. A valid Discord bot token is typically at least 30 characters long.", "OK");
                        Preferences.Default.Set("SlackordBotToken", token);
                    }
                }
                else
                {
                    Preferences.Default.Set("SlackordBotToken", string.Empty);
                }
            }

            if (LogLevelPicker.SelectedIndex >= 0)
            {
                Preferences.Default.Set("DiscordLogLevel", LogLevelPicker.SelectedIndex);
                DiscordBot.Instance.UpdateLogLevel(GetLogSeverityFromIndex(LogLevelPicker.SelectedIndex));
            }

            if (CleanupAfterImportPicker.SelectedIndex >= 0)
            {
                Preferences.Default.Set("CleanupAfterImport", CleanupAfterImportPicker.SelectedIndex);
            }

            Preferences.Default.Set("CheckForUpdatesOnStartup", CheckUpdatesSwitch.IsToggled);
            await UpdateMainPageUISettings();

            if (tokenChanged)
            {
                await new ApplicationWindow().CheckForValidBotToken();
            }

            await DisplayAlertAsync("Success", "Settings saved successfully!", "OK");
            await Navigation.PopAsync();
        }

        /// <summary>
        /// Resets all settings on the Options page to default values
        /// </summary>
        private async void ResetButton_Clicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlertAsync("Confirm Reset",
                "Are you sure you want to reset all settings to default values?",
                "Yes", "No");

            if (confirm)
            {
                UserFormatPicker.SelectedIndex = 0;
                TimestampFormatPicker.SelectedIndex = 0;
                BotTokenEntry.Text = string.Empty;
                LogLevelPicker.SelectedIndex = 3;
                CleanupAfterImportPicker.SelectedIndex = (int)CleanupBehavior.Prompt;
                CheckUpdatesSwitch.IsToggled = true;

                await DisplayAlertAsync("Reset Complete", "All settings have been reset to default values.", "OK");
            }
        }

        /// <summary>
        /// Cancels the settings edit and returns to the previous page
        /// </summary>
        private async void CancelButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        /// <summary>
        /// Displays the about dialog
        /// </summary>
        private void About_Clicked(object sender, EventArgs e)
        {
            ApplicationWindow.DisplayAbout();
        }

        /// <summary>
        /// Displays the donation prompt and opens the donation URL if accepted
        /// </summary>
        private async void Donate_Clicked(object sender, EventArgs e)
        {
            await ApplicationWindow.CreateDonateAlert();
        }

        /// <summary>
        /// Enables or disables bot token editing based on connection state
        /// </summary>
        public void UpdateBotTokenEditability()
        {
            ConnectionState connectionState = DiscordBot.Instance.GetClientConnectionState();
            bool isDisconnected = connectionState == ConnectionState.Disconnected;
            BotTokenEntry.IsEnabled = isDisconnected;

            if (!isDisconnected)
            {
                BotTokenEntry.Placeholder = "Disconnect bot before changing token";
            }
            else
            {
                BotTokenEntry.Placeholder = "Enter your Discord bot token";
            }
        }

        /// <summary>
        /// Updates UI settings on the main page based on saved preferences
        /// </summary>
        private static async Task UpdateMainPageUISettings()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ApplicationWindow.GetUserFormatValue();
                await ApplicationWindow.GetTimeStampValue();
                await new ApplicationWindow().CheckForValidBotToken();
            });
        }

        /// <summary>
        /// Validates the format and length of a Discord bot token
        /// </summary>
        private static bool ValidateBotToken(string token)
        {
            return !string.IsNullOrEmpty(token) && token.Trim().Length >= 30;
        }

        /// <summary>
        /// Opens the Discord invite link
        /// </summary>
        private void OnLabelClicked(object sender, TappedEventArgs e)
        {
            try
            {
                var uri = new Uri("https://discord.gg/yccMweYPN8");
                Launcher.Default.OpenAsync(uri);
            }
            catch (Exception ex)
            {
                Application.Current.Windows[0].Page.DisplayAlertAsync("Error", $"Unable to open link: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Maps the selected log level index to the corresponding Discord LogSeverity
        /// </summary>
        private static LogSeverity GetLogSeverityFromIndex(int index)
        {
            return index switch
            {
                0 => LogSeverity.Critical,
                1 => LogSeverity.Error,
                2 => LogSeverity.Warning,
                3 => LogSeverity.Info,
                4 => LogSeverity.Debug,
                5 => LogSeverity.Verbose,
                _ => LogSeverity.Info
            };
        }

        /// <summary>
        /// Returns a human-readable description of a log level
        /// </summary>
        public static string GetLogLevelDescription(int logLevel)
        {
            return logLevel switch
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

        /// <summary>
        /// Gets the current cleanup behavior setting from preferences
        /// </summary>
        public static CleanupBehavior GetCleanupBehavior()
        {
            int behaviorIndex = Preferences.Default.Get("CleanupAfterImport", (int)CleanupBehavior.Prompt);
            return (CleanupBehavior)behaviorIndex;
        }

        /// <summary>
        /// Returns a human-readable description of the cleanup behavior
        /// </summary>
        public static string GetCleanupBehaviorDescription(CleanupBehavior behavior)
        {
            return behavior switch
            {
                CleanupBehavior.Prompt => "Prompt - Ask before cleaning up",
                CleanupBehavior.Automatically => "Automatically - Clean up without asking",
                CleanupBehavior.Never => "Never - Keep all files",
                _ => "Prompt - Ask before cleaning up"
            };
        }
    }
}