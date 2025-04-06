using Discord;
using MenuApp;
using System.Text;

namespace Slackord.Classes
{
    public class ApplicationWindow
    {
        private static MainPage MainPageInstance => MenuApp.MainPage.Current;
        private readonly DiscordBot _discordBot = DiscordBot.Instance;
        private CancellationTokenSource cancellationTokenSource;
        // Remove the isFirstRun field completely
        private bool hasValidBotToken;
        private string DiscordToken;
        private bool hasEverBeenConnected = false;
        public static UserFormatOrder CurrentUserFormatOrder { get; set; } = UserFormatOrder.DisplayName_User_RealName;
        public enum UserFormatOrder
        {
            DisplayName_User_RealName,
            DisplayName_RealName_User,
            User_DisplayName_RealName,
            User_RealName_DisplayName,
            RealName_DisplayName_User,
            RealName_User_DisplayName
        }

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

        // Make CheckForPartialImport static to match how it's called in MainPage
        public static async Task CheckForPartialImport()
        {
            bool hasPartialImport = Preferences.Default.Get("HasPartialImport", false);
            bool enableResumeImport = Preferences.Default.Get("EnableResumeImport", true);

            if (hasPartialImport && enableResumeImport)
            {
                string lastImportType = Preferences.Default.Get("LastImportType", string.Empty);
                string lastImportChannel = Preferences.Default.Get("LastImportChannel", string.Empty);

                bool shouldResume = await MenuApp.MainPage.Current.DisplayAlert(
                    "Resume Import",
                    $"A previous {(lastImportType == "Full" ? "server" : "channel")} import was interrupted. Would you like to resume from where it left off?",
                    "Resume", "Start New");

                if (shouldResume)
                {
                    await ResumeImport.CheckForPartialImport();
                }
                else
                {
                    // User chose not to resume, clear the partial import state
                    ResumeImport.ClearResumeState();
                }
            }
        }

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

        public static async Task SetTimestampValue()
        {
            string currentSetting = Preferences.Default.Get("TimestampValue", "12 Hour");
            string newSetting = currentSetting == "12 Hour" ? "24 Hour" : "12 Hour";
            Preferences.Default.Set("TimestampValue", newSetting);
            WriteToDebugWindow($"Timestamp setting changed to {newSetting}. Messages sent to Discord will use this format.\n");

            await Task.CompletedTask;
        }

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

        public static async Task SetUserFormatValue()
        {
            try
            {
                UserFormatOrder currentSetting = Enum.Parse<UserFormatOrder>(Preferences.Default.Get("UserFormatValue", CurrentUserFormatOrder.ToString()));
                int nextSettingIndex = ((int)currentSetting + 1) % Enum.GetNames(typeof(UserFormatOrder)).Length;
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

        public async Task ImportJsonAsync(bool isFullExport)
        {
            // Create a new cancellation token source
            using var cts = new CancellationTokenSource();
            // Use proper assignment to avoid the IDE0059 warning
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

        public void CancelImport()
        {
            cancellationTokenSource?.Cancel();
        }

        public async Task ToggleDiscordConnection()
        {
            // Check for valid bot token before proceeding
            if (!hasValidBotToken)
            {
                await ChangeBotConnectionButton("Connect", new Microsoft.Maui.Graphics.Color(128, 128, 128), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                WriteToDebugWindow("Your bot token doesn't look valid. Please go to the Options page to enter a valid token.");

                // Navigate to options page
                if (Application.Current.MainPage is NavigationPage navPage)
                {
                    await navPage.PushAsync(new Slackord.Pages.OptionsPage());
                }
                return;
            }

            var currentState = _discordBot.GetClientConnectionState();

            // Disconnect if currently connected
            if (currentState == ConnectionState.Connected)
            {
                await ChangeBotConnectionButton("Disconnecting", new Microsoft.Maui.Graphics.Color(255, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await _discordBot.LogoutClientAsync();
                await ChangeBotConnectionButton("Disconnected", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                hasEverBeenConnected = true;
                return;
            }

            // Reconnect using MainAsync() if the client is not connected
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

                // Poll the connection state until it resolves to either Connected or it fails to connect
                int maxAttempts = 10;
                int attempt = 0;
                do
                {
                    await Task.Delay(1000); // Poll every second
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

        public static async Task CheckForNewVersion(bool isStartupCheck)
        {
            await UpdateCheck.CheckForUpdates(isStartupCheck);
        }

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

        public static void ClearLog()
        {
            MainPage.DebugWindowInstance.Text = string.Empty;
        }

        private static readonly StringBuilder debugOutput = new();
        public static void WriteToDebugWindow(string text)
        {
            _ = debugOutput.Append(text);
            PushDebugText();
        }

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

        // Modified to remove unused parameter
        public static async Task ToggleBotTokenEnable(bool isEnabled)
        {
            Preferences.Default.Set("BotTokenEnabled", isEnabled);

            // If the OptionsPage is currently visible, we should update its UI
            // Fix for CA1826 warning - use indexer instead of LastOrDefault() LINQ method
            var navigationStack = Application.Current.MainPage?.Navigation?.NavigationStack;
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

        public static void UpdateProgressBar(int current, int total, string type)
        {
            double progressValue = (double)current / total;

            if (MainPage.Current != null)
            {
                _ = MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MainPage.ProgressBarInstance.Progress = progressValue;
                    MainPage.ProgressBarTextInstance.Text = $"Completed {current} out of {total} {type}.";
                });
            }
        }

        public static void ResetProgressBar()
        {
            // Reset the progress bar value to 0.
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