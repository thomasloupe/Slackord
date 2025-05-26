using Discord;
using MenuApp;
using System.Text;

namespace Slackord.Classes
{
    /// <summary>
    /// Main application window class responsible for managing UI interactions, Discord bot connection, 
    /// and coordinating import/export operations
    /// </summary>
    public class ApplicationWindow
    {
        /// <summary>
        /// Gets the current MainPage instance
        /// </summary>
        private static MainPage MainPageInstance => MainPage.Current;

        /// <summary>
        /// Discord bot instance for handling Discord operations
        /// </summary>
        private readonly DiscordBot _discordBot = DiscordBot.Instance;

        /// <summary>
        /// Cancellation token source for managing operation cancellation
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Indicates whether the stored bot token is valid
        /// </summary>
        private bool hasValidBotToken;

        /// <summary>
        /// The Discord bot token for authentication
        /// </summary>
        private string DiscordToken;

        /// <summary>
        /// Tracks whether the bot has ever been connected to prevent UI confusion
        /// </summary>
        private bool hasEverBeenConnected = false;

        /// <summary>
        /// Gets or sets the current user format order for displaying user names
        /// </summary>
        public static UserFormatOrder CurrentUserFormatOrder { get; set; } = UserFormatOrder.DisplayName_User_RealName;

        /// <summary>
        /// Defines the order in which user name components are displayed
        /// </summary>
        public enum UserFormatOrder
        {
            DisplayName_User_RealName,
            DisplayName_RealName_User,
            User_DisplayName_RealName,
            User_RealName_DisplayName,
            RealName_DisplayName_User,
            RealName_User_DisplayName
        }

        /// <summary>
        /// Checks if this is the first run of the application and displays appropriate welcome message
        /// </summary>
        public static void CheckForFirstRun()
        {
            if (Preferences.Default.ContainsKey("FirstRun"))
            {
                if (Preferences.Default.Get("FirstRun", true))
                {
                    WriteToDebugWindow("Welcome to Slackord!\n");
                    Preferences.Default.Set("FirstRun", false);
                }
                else
                {
                    MainPage.DebugWindowInstance.Text += "Welcome back to Slackord!\n";
                }
            }
            else
            {
                Preferences.Default.Set("FirstRun", true);
                WriteToDebugWindow("Welcome to Slackord!\n");
            }
        }

        /// <summary>
        /// Validates the stored Discord bot token and updates the connection button state accordingly
        /// </summary>
        public async Task CheckForValidBotToken()
        {
            DiscordToken = Preferences.Default.Get("SlackordBotToken", string.Empty).Trim();
            if (DiscordToken.Length > 30)
            {
                await ChangeBotConnectionButton("Connect", new Microsoft.Maui.Graphics.Color(255, 69, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                hasValidBotToken = true;
                WriteToDebugWindow("Slackord found and is using an existing Discord token!\n");
            }
            else
            {
                await ChangeBotConnectionButton("Connect", new Microsoft.Maui.Graphics.Color(128, 128, 128), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                hasValidBotToken = false;
                WriteToDebugWindow("Slackord needs a valid Discord bot token. Please go to Options to enter your token.\n\n");
            }
        }

        /// <summary>
        /// Checks for incomplete import sessions and asks user if they want to resume
        /// </summary>
        public static async Task CheckForPartialImport()
        {
            try
            {
                bool enableResumeImport = Preferences.Default.Get("EnableResumeImport", true);
                if (!enableResumeImport)
                {
                    return;
                }

                var incompleteSessions = ImportSession.GetIncompleteImports();
                if (incompleteSessions.Count == 0)
                {
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("Found incomplete import sessions:");
                sb.AppendLine();

                for (int i = 0; i < Math.Min(incompleteSessions.Count, 3); i++)
                {
                    var session = incompleteSessions[i];
                    var incompleteChannels = session.Channels.Where(c => !c.IsCompleted).Count();
                    var totalChannels = session.Channels.Count;

                    sb.AppendLine($"📅 {session.SessionId}:");
                    sb.AppendLine($"   • {incompleteChannels}/{totalChannels} channels incomplete");

                    if (incompleteChannels > 0)
                    {
                        var firstIncomplete = session.Channels.FirstOrDefault(c => !c.IsCompleted);
                        if (firstIncomplete != null)
                        {
                            sb.AppendLine($"   • Next: {firstIncomplete.GetProgressDisplay()}");
                        }
                    }
                    sb.AppendLine();
                }

                if (incompleteSessions.Count > 3)
                {
                    sb.AppendLine($"... and {incompleteSessions.Count - 3} more sessions");
                }

                bool shouldResume = await MainPage.Current.DisplayAlert(
                    "Resume Import Sessions",
                    sb.ToString() + "Would you like to resume the most recent incomplete import?",
                    "Resume", "Start New");

                if (shouldResume)
                {
                    await ResumeRecentImport(incompleteSessions.First());
                }
                else
                {
                    WriteToDebugWindow("💡 You can resume imports later using the '/resume' Discord command.\n\n");
                }
            }
            catch (Exception ex)
            {
                WriteToDebugWindow($"❌ Error checking for partial imports: {ex.Message}\n");
                Logger.Log($"CheckForPartialImport error: {ex.Message}");
            }
        }

        /// <summary>
        /// Resumes the most recent incomplete import session
        /// </summary>
        /// <param name="sessionToResume">The import session to resume</param>
        private static async Task ResumeRecentImport(ImportSession sessionToResume)
        {
            try
            {
                WriteToDebugWindow($"🔄 Preparing to resume import session: {sessionToResume.SessionId}\n");

                var incompleteChannels = sessionToResume.Channels.Where(c => !c.IsCompleted).ToList();
                var totalMessages = incompleteChannels.Sum(c => c.MessagesRemaining);

                WriteToDebugWindow($"📊 Resume Summary:\n");
                WriteToDebugWindow($"   • Session: {sessionToResume.SessionId}\n");
                WriteToDebugWindow($"   • Incomplete channels: {incompleteChannels.Count}\n");
                WriteToDebugWindow($"   • Messages remaining: {totalMessages:N0}\n\n");

                foreach (var channel in incompleteChannels.Take(5))
                {
                    WriteToDebugWindow($"   📁 {channel.GetProgressDisplay()}\n");
                }

                if (incompleteChannels.Count > 5)
                {
                    WriteToDebugWindow($"   ... and {incompleteChannels.Count - 5} more channels\n");
                }

                WriteToDebugWindow($"\n");

                ImportJson.SetCurrentSession(sessionToResume);

                WriteToDebugWindow($"✅ Session loaded! Ready to resume Discord import.\n");
                WriteToDebugWindow($"🚀 Use the Discord '/slackord' or '/resume' command to continue posting messages.\n\n");

                ProcessingManager.Instance.SetState(ProcessingState.ReadyForDiscordImport);

                bool shouldShowInstructions = await MainPage.Current.DisplayAlert(
                    "Resume Ready",
                    $"Session {sessionToResume.SessionId} is now loaded and ready to resume.\n\n" +
                    $"To continue:\n" +
                    $"1. Connect to Discord if not already connected\n" +
                    $"2. Use the '/slackord' or '/resume' slash command in Discord\n\n" +
                    $"The system will automatically continue from where it left off.",
                    "Got it", "Show Details");

                if (shouldShowInstructions)
                {
                    ShowResumeDetails(sessionToResume);
                }
            }
            catch (Exception ex)
            {
                WriteToDebugWindow($"❌ Error resuming import: {ex.Message}\n");
                Logger.Log($"ResumeRecentImport error: {ex.Message}");
                await MainPage.Current.DisplayAlert("Resume Error", $"An error occurred while resuming: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Shows detailed information about what will be resumed
        /// </summary>
        /// <param name="session">The import session to show details for</param>
        private static void ShowResumeDetails(ImportSession session)
        {
            WriteToDebugWindow($"📋 Detailed Resume Information:\n");
            WriteToDebugWindow($"Session Path: {session.SessionPath}\n\n");

            foreach (var channel in session.Channels)
            {
                WriteToDebugWindow($"📁 {channel.Name}:\n");
                WriteToDebugWindow($"   • Status: {(channel.IsCompleted ? "✅ Complete" : "🔄 In Progress")}\n");
                WriteToDebugWindow($"   • Progress: {channel.MessagesSent:N0}/{channel.TotalMessages:N0} messages\n");

                if (!channel.IsCompleted)
                {
                    WriteToDebugWindow($"   • Remaining: {channel.MessagesRemaining:N0} messages\n");
                }

                if (channel.DiscordChannelId > 0)
                {
                    WriteToDebugWindow($"   • Discord Channel ID: {channel.DiscordChannelId}\n");
                }

                WriteToDebugWindow($"   • File: {Path.GetFileName(session.GetChannelFilePath(channel.Name))}\n");

                string fileSize = SlackordFileManager.GetFileSizeDisplay(session.GetChannelFilePath(channel.Name));
                WriteToDebugWindow($"   • File Size: {fileSize}\n\n");
            }
        }

        /// <summary>
        /// Retrieves and displays the current timestamp setting
        /// </summary>
        public static async Task GetTimeStampValue()
        {
            if (!Preferences.Default.ContainsKey("TimestampValue"))
            {
                Preferences.Default.Set("TimestampValue", "12 Hour");
            }

            string timestampValue = Preferences.Default.Get("TimestampValue", "12 Hour");
            WriteToDebugWindow($"Timestamp setting: {timestampValue}\n");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Toggles the timestamp format between 12-hour and 24-hour
        /// </summary>
        public static async Task SetTimestampValue()
        {
            string currentSetting = Preferences.Default.Get("TimestampValue", "12 Hour");
            string newSetting = currentSetting == "12 Hour" ? "24 Hour" : "12 Hour";
            Preferences.Default.Set("TimestampValue", newSetting);
            WriteToDebugWindow($"Timestamp setting changed to {newSetting}. Messages sent to Discord will use this format.\n");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Retrieves and displays the current user format setting
        /// </summary>
        public static async Task GetUserFormatValue()
        {
            if (!Preferences.Default.ContainsKey("UserFormatValue"))
            {
                Preferences.Default.Set("UserFormatValue", UserFormatOrder.DisplayName_User_RealName.ToString());
            }

            string userFormatValue = Preferences.Default.Get("UserFormatValue", CurrentUserFormatOrder.ToString());
            UserFormatOrder currentSetting = Enum.Parse<UserFormatOrder>(userFormatValue);
            CurrentUserFormatOrder = currentSetting;
            string fullFormat = currentSetting.ToString().Replace("_", " > ");
            WriteToDebugWindow($"User Format setting: {fullFormat}\n");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Cycles to the next user format setting
        /// </summary>
        public static async Task SetUserFormatValue()
        {
            try
            {
                UserFormatOrder currentSetting = Enum.Parse<UserFormatOrder>(Preferences.Default.Get("UserFormatValue", CurrentUserFormatOrder.ToString()));
                int nextSettingIndex = ((int)currentSetting + 1) % Enum.GetNames<UserFormatOrder>().Length;
                UserFormatOrder nextSetting = (UserFormatOrder)nextSettingIndex;

                Preferences.Default.Set("UserFormatValue", nextSetting.ToString());
                CurrentUserFormatOrder = nextSetting;

                await GetUserFormatValue();
            }
            catch (Exception ex)
            {
                WriteToDebugWindow($"Error in SetUserFormatValue: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves and displays the current Discord log level setting
        /// </summary>
        public static async Task GetDiscordLogLevelValue()
        {
            if (!Preferences.Default.ContainsKey("DiscordLogLevel"))
            {
                Preferences.Default.Set("DiscordLogLevel", 3);
            }

            int logLevel = Preferences.Default.Get("DiscordLogLevel", 3);
            string logLevelName = GetDiscordLogLevelName(logLevel);
            WriteToDebugWindow($"Discord Log Level: {logLevelName}\n");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Cycles to the next Discord log level setting
        /// </summary>
        public static async Task SetDiscordLogLevelValue()
        {
            try
            {
                int currentLogLevel = Preferences.Default.Get("DiscordLogLevel", 3);
                int nextLogLevel = (currentLogLevel + 1) % 6;

                Preferences.Default.Set("DiscordLogLevel", nextLogLevel);

                LogSeverity newSeverity = GetLogSeverityFromLevel(nextLogLevel);
                DiscordBot.Instance.UpdateLogLevel(newSeverity);

                await GetDiscordLogLevelValue();
            }
            catch (Exception ex)
            {
                WriteToDebugWindow($"Error in SetDiscordLogLevelValue: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts a numeric log level to a human-readable name
        /// </summary>
        /// <param name="logLevel">The numeric log level (0-5)</param>
        /// <returns>The human-readable log level name</returns>
        private static string GetDiscordLogLevelName(int logLevel)
        {
            return logLevel switch
            {
                0 => "Critical",
                1 => "Error",
                2 => "Warning",
                3 => "Info",
                4 => "Debug",
                5 => "Verbose",
                _ => "Info"
            };
        }

        /// <summary>
        /// Converts a numeric log level to a LogSeverity enum value
        /// </summary>
        /// <param name="logLevel">The numeric log level (0-5)</param>
        /// <returns>The corresponding LogSeverity enum value</returns>
        private static LogSeverity GetLogSeverityFromLevel(int logLevel)
        {
            return logLevel switch
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
        /// Initiates the JSON import process for Slack data
        /// </summary>
        /// <param name="isFullExport">Whether this is a full export or single channel export</param>
        public async Task ImportJsonAsync(bool isFullExport)
        {
            using var cts = new CancellationTokenSource();
            cancellationTokenSource = cts;
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            try
            {
                await ImportJson.ImportJsonAsync(isFullExport, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                cancellationTokenSource?.Cancel();
            }
        }

        /// <summary>
        /// Cancels all ongoing import and Discord operations
        /// </summary>
        public void CancelImport()
        {
            _discordBot.CancelDiscordOperations();

            Application.Current.Dispatcher.Dispatch(() =>
            {
                ApplicationWindow.WriteToDebugWindow("🛑 Cancellation requested for all operations...\n");
            });

            cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Toggles the Discord bot connection state and handles various connection scenarios
        /// </summary>
        public async Task ToggleDiscordConnection()
        {
            if (!hasValidBotToken)
            {
                await ChangeBotConnectionButton("Connect", new Microsoft.Maui.Graphics.Color(128, 128, 128), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                WriteToDebugWindow("Your bot token doesn't look valid. Please go to the Options page to enter a valid token.");

                if (Application.Current.Windows[0].Page is NavigationPage navPage)
                {
                    await navPage.PushAsync(new Pages.OptionsPage());
                }
                return;
            }

            var currentState = _discordBot.GetClientConnectionState();

            if (ProcessingManager.Instance.CurrentState == ProcessingState.ImportingToDiscord)
            {
                await ChangeBotConnectionButton("Cancelling", new Microsoft.Maui.Graphics.Color(255, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                WriteToDebugWindow("Cancelling Discord message posting...\n");

                CancelImport();

                Preferences.Default.Set("HasPartialImport", true);

                return;
            }

            if (currentState == ConnectionState.Connected)
            {
                await ChangeBotConnectionButton("Disconnecting", new Microsoft.Maui.Graphics.Color(255, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await _discordBot.LogoutClientAsync();
                await ChangeBotConnectionButton("Disconnected", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                hasEverBeenConnected = true;
                return;
            }

            if (currentState == ConnectionState.Disconnected)
            {
                string buttonLabel = hasEverBeenConnected ? "Reconnecting" : "Connecting";
                var backgroundColor = hasEverBeenConnected ? new Microsoft.Maui.Graphics.Color(255, 0, 0) : new Microsoft.Maui.Graphics.Color(255, 255, 0);
                await ChangeBotConnectionButton(buttonLabel, backgroundColor, new Microsoft.Maui.Graphics.Color(0, 0, 0));

                if (_discordBot.DiscordClient == null)
                {
                    await _discordBot.MainAsync(DiscordToken);
                }
                else
                {
                    await _discordBot.StartClientAsync(DiscordToken);
                }

                int maxAttempts = 10;
                int attempt = 0;
                do
                {
                    await Task.Delay(1000);
                    currentState = _discordBot.GetClientConnectionState();
                    attempt++;
                } while (currentState != ConnectionState.Connected && currentState != ConnectionState.Disconnected && attempt < maxAttempts);

                if (currentState == ConnectionState.Connected)
                {
                    hasEverBeenConnected = true;
                    await ChangeBotConnectionButton("Connected", new Microsoft.Maui.Graphics.Color(0, 255, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                }
                else if (currentState == ConnectionState.Disconnected)
                {
                    await ChangeBotConnectionButton("Connect", new Microsoft.Maui.Graphics.Color(255, 69, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                }
                else
                {
                    await ChangeBotConnectionButton("Failed to Connect", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                }
            }
        }

        /// <summary>
        /// Handles UI updates when an operation is cancelled
        /// </summary>
        public static async Task OnOperationCancelled()
        {
            await ChangeBotConnectionButton("Cancelled", new Microsoft.Maui.Graphics.Color(255, 165, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
            WriteToDebugWindow("Discord operation cancelled successfully.\n");
        }

        /// <summary>
        /// Checks for application updates from GitHub
        /// </summary>
        /// <param name="isStartupCheck">Whether this is an automatic startup check or manual check</param>
        public static async Task CheckForNewVersion(bool isStartupCheck)
        {
            await UpdateCheck.CheckForUpdates(isStartupCheck);
        }

        /// <summary>
        /// Displays the application about dialog with version and author information
        /// </summary>
        public static void DisplayAbout()
        {
            string currentVersion = Version.GetVersion();
            _ = MainPageInstance.DisplayAlert("", $@"
Slackord {currentVersion}.
Created by Thomas Loupe.
Github: https://github.com/thomasloupe
Twitter: https://twitter.com/acid_rain
Website: https://thomasloupe.com
", "OK");
        }

        /// <summary>
        /// Creates and displays a donation request dialog
        /// </summary>
        public static async Task CreateDonateAlert()
        {
            string url = "https://paypal.me/thomasloupe";
            string message = @"
Slackord will always be free!
If you'd like to buy me a beer anyway, I won't tell you not to!
Would you like to open the donation page now?
";

            bool result = await MainPageInstance.DisplayAlert("Slackord is free, but beer is not!", message, "Yes", "No");

            if (result)
            {
                _ = await Launcher.OpenAsync(new Uri(url));
            }
        }

        /// <summary>
        /// Prompts user for confirmation and exits the application
        /// </summary>
        public static async Task ExitApplication()
        {
            bool result = await MainPageInstance.DisplayAlert("Confirm Exit", "Are you sure you want to quit Slackord?", "Yes", "No");

            if (result)
            {
                DevicePlatform operatingSystem = DeviceInfo.Platform;
                if (operatingSystem == DevicePlatform.MacCatalyst)
                {
                    _ = System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
                }
                else if (operatingSystem == DevicePlatform.WinUI)
                {
                    Application.Current.Quit();
                }
            }
        }

        /// <summary>
        /// Copies the entire debug log content to the system clipboard
        /// </summary>
        public static void CopyLog()
        {
            _ = MainPage.DebugWindowInstance.Focus();
            MainPage.DebugWindowInstance.CursorPosition = 0;
            MainPage.DebugWindowInstance.SelectionLength = MainPage.DebugWindowInstance.Text.Length;

            string selectedText = MainPage.DebugWindowInstance.Text;

            if (!string.IsNullOrEmpty(selectedText))
            {
                _ = Clipboard.SetTextAsync(selectedText);
            }
        }

        /// <summary>
        /// Clears all content from the debug window
        /// </summary>
        public static void ClearLog()
        {
            MainPage.DebugWindowInstance.Text = string.Empty;
        }

        /// <summary>
        /// StringBuilder for accumulating debug output before pushing to UI
        /// </summary>
        private static readonly StringBuilder debugOutput = new();

        /// <summary>
        /// Writes text to the debug window, handling thread safety
        /// </summary>
        /// <param name="text">The text to write to the debug window</param>
        public static void WriteToDebugWindow(string text)
        {
            _ = debugOutput.Append(text);
            PushDebugText();
        }

        /// <summary>
        /// Pushes accumulated debug text to the UI thread-safely
        /// </summary>
        public static void PushDebugText()
        {
            if (!MainThread.IsMainThread)
            {
                MainThread.BeginInvokeOnMainThread(PushDebugText);
                return;
            }

            MainPage.DebugWindowInstance.Text += debugOutput.ToString();
            _ = debugOutput.Clear();
        }

        /// <summary>
        /// Updates the Discord connection button appearance and text
        /// </summary>
        /// <param name="state">The text to display on the button</param>
        /// <param name="backgroundColor">The background color of the button</param>
        /// <param name="textColor">The text color of the button</param>
        public static async Task ChangeBotConnectionButton(string state, Microsoft.Maui.Graphics.Color backgroundColor, Microsoft.Maui.Graphics.Color textColor)
        {
            if (MainPage.Current != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var button = MainPage.BotConnectionButtonInstance;
                    button.Text = state;
                    button.TextColor = textColor;
                    button.BackgroundColor = backgroundColor;
                });
            }
        }

        /// <summary>
        /// Toggles the bot token input field enabled state and updates the options page if visible
        /// </summary>
        /// <param name="isEnabled">Whether the bot token field should be enabled</param>
        public static async Task ToggleBotTokenEnable(bool isEnabled)
        {
            Preferences.Default.Set("BotTokenEnabled", isEnabled);

            var navigationStack = Application.Current.Windows[0].Page?.Navigation?.NavigationStack;
            if (navigationStack != null && navigationStack.Count > 0 &&
                navigationStack[navigationStack.Count - 1] is Pages.OptionsPage optionsPage)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    optionsPage.UpdateBotTokenEditability();
                });
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Makes the progress bar visible in the UI
        /// </summary>
        public static void ShowProgressBar()
        {
            if (MainPage.Current != null)
            {
                _ = MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MainPage.ProgressBarInstance.IsVisible = true;
                    MainPage.ProgressBarTextInstance.IsVisible = true;
                });
            }
        }

        /// <summary>
        /// Hides the progress bar from the UI
        /// </summary>
        public static void HideProgressBar()
        {
            if (MainPage.Current != null)
            {
                _ = MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MainPage.ProgressBarInstance.IsVisible = false;
                    MainPage.ProgressBarTextInstance.IsVisible = false;
                });
            }
        }

        /// <summary>
        /// Checks if Discord import can be started based on current processing state
        /// </summary>
        /// <returns>True if ready for Discord import, false otherwise</returns>
        public static bool CanStartDiscordImport()
        {
            return ProcessingManager.Instance.CanStartDiscordImport;
        }

        /// <summary>
        /// Updates the progress bar with custom text and percentage
        /// </summary>
        /// <param name="current">Current progress value</param>
        /// <param name="total">Total expected value</param>
        /// <param name="customText">Custom text to display</param>
        public static void UpdateProgressBarWithCustomText(int current, int total, string customText)
        {
            double progressValue = total > 0 ? (double)current / total : 0;

            if (MainPage.Current != null)
            {
                _ = MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MainPage.ProgressBarInstance.Progress = progressValue;
                    MainPage.ProgressBarTextInstance.Text = customText;
                });
            }
        }

        /// <summary>
        /// Updates the progress bar with percentage display
        /// </summary>
        /// <param name="current">Current progress value</param>
        /// <param name="total">Total expected value</param>
        /// <param name="type">Type of items being processed (e.g., "messages", "files")</param>
        public static void UpdateProgressBar(int current, int total, string type)
        {
            double progressValue = total > 0 ? (double)current / total : 0;

            if (MainPage.Current != null)
            {
                _ = MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MainPage.ProgressBarInstance.Progress = progressValue;

                    double percentage = progressValue * 100;
                    MainPage.ProgressBarTextInstance.Text = $"Completed {current:N0} out of {total:N0} {type} ({percentage:F1}%)";
                });
            }
        }

        /// <summary>
        /// Resets the progress bar to its initial state
        /// </summary>
        public static void ResetProgressBar()
        {
            if (MainPage.Current != null)
            {
                _ = MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MainPage.ProgressBarInstance.Progress = 0;
                    MainPage.ProgressBarTextInstance.Text = string.Empty;
                });
            }
        }
    }
}