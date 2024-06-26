using Discord;
using Slackord.Classes;
using System.Text;

namespace MenuApp
{
    public class ApplicationWindow
    {
        private static MainPage MainPageInstance => MainPage.Current;
        private readonly DiscordBot _discordBot = DiscordBot.Instance;
        private CancellationTokenSource cancellationTokenSource;
        private bool isFirstRun;
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

        public void CheckForFirstRun()
        {
            if (Preferences.Default.ContainsKey("FirstRun"))
            {
                if (Preferences.Default.Get("FirstRun", true))
                {
                    WriteToDebugWindow("Welcome to Slackord!\n");
                    Preferences.Default.Set("FirstRun", false);
                    isFirstRun = true;
                }
                else
                {
                    MainPage.DebugWindowInstance.Text += "Welcome back to Slackord!\n";
                    isFirstRun = false;
                }
            }
            else
            {
                Preferences.Default.Set("FirstRun", true);
                isFirstRun = true;
                WriteToDebugWindow("Welcome to Slackord!\n");
            }
        }

        public async Task CheckForValidBotToken()
        {
            if (isFirstRun)
            {
                if (Preferences.Default.ContainsKey("SlackordBotToken"))
                {
                    DiscordToken = Preferences.Default.Get("SlackordBotToken", string.Empty).Trim();
                    if (DiscordToken.Length > 30)
                    {
                        await ChangeBotConnectionButton("Connect", new Microsoft.Maui.Graphics.Color(255, 69, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                        hasValidBotToken = true;
                        WriteToDebugWindow("Slackord found an existing valid bot token, and will use it.\n");
                    }
                    else
                    {
                        await ChangeBotConnectionButton("Connect", new Microsoft.Maui.Graphics.Color(128, 128, 128), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                        hasValidBotToken = false;
                        WriteToDebugWindow("""
                            Slackord tried to load your last token, but wasn't successful. Please re-enter a new, valid token.

                            """);
                    }
                }
                else
                {
                    Preferences.Default.Set("SlackordBotToken", string.Empty);
                    await ChangeBotConnectionButton("Connect", new Microsoft.Maui.Graphics.Color(128, 128, 128), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                    hasValidBotToken = false;
                    WriteToDebugWindow("Please enter a valid bot token to enable bot connection.\n");
                }
            }
            else
            {
                DiscordToken = Preferences.Default.Get("SlackordBotToken", string.Empty).Trim();
                if (DiscordToken.Length > 30)
                {
                    await ChangeBotConnectionButton("Connect", new Microsoft.Maui.Graphics.Color(255, 69, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                    hasValidBotToken = true;
                    WriteToDebugWindow("Slackord found an existing valid bot token, and will use it.\n");
                }
                else
                {
                    await ChangeBotConnectionButton("Connect", new Microsoft.Maui.Graphics.Color(128, 128, 128), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                    hasValidBotToken = false;
                    WriteToDebugWindow("""
                        Slackord tried to load your last token, but wasn't successful. Please re-enter a new, valid token.

                        """);
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
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                MainPage.TimeStampButtonInstance.Text = "Timestamp: " + timestampValue;
            });

            WriteToDebugWindow($"Current Timestamp setting is {timestampValue}. Messages sent to Discord will use this format.\n");
        }

        public static async Task SetTimestampValue()
        {
            string currentSetting = Preferences.Default.Get("TimestampValue", "12 Hour");
            string newSetting = currentSetting == "12 Hour" ? "24 Hour" : "12 Hour";
            Preferences.Default.Set("TimestampValue", newSetting);
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                MainPage.TimeStampButtonInstance.Text = "Timestamp: " + newSetting;
            });

            WriteToDebugWindow($"Timestamp setting changed to {newSetting}. Messages sent to Discord will use this format.\n");
        }

        public static async Task GetUserFormatValue()
        {
            if (!Preferences.Default.ContainsKey("UserFormatValue"))
            {
                Preferences.Default.Set("UserFormatValue", UserFormatOrder.DisplayName_User_RealName.ToString());
            }

            string userFormatValue = Preferences.Default.Get("UserFormatValue", CurrentUserFormatOrder.ToString());
            UserFormatOrder currentSetting = Enum.Parse<UserFormatOrder>(userFormatValue);
            
            string shorthand = currentSetting.ToString().Replace("_", " > ")
                                                        .Replace("DisplayName", "D")
                                                        .Replace("User", "U")
                                                        .Replace("RealName", "R");

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                MainPage.UserFormatButtonInstance.Text = "User Format: " + shorthand;
            });

            string fullFormat = currentSetting.ToString().Replace("_", " > ");
            WriteToDebugWindow($"Current User Format setting is {fullFormat}. Messages sent to Discord will use this format.\n");
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
            cancellationTokenSource = new CancellationTokenSource();
            using CancellationTokenSource cts = new();
            Interlocked.Exchange(ref cancellationTokenSource, cts)?.Cancel();
            CancellationToken cancellationToken = cts.Token;
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

        public async Task CreateBotTokenPrompt()
        {
            string discordToken = await MainPageInstance.DisplayPromptAsync("Enter Bot Token", "Please enter your bot's token:", "OK", "Cancel", maxLength: 90);

            if (!string.IsNullOrEmpty(discordToken))
            {
                if (discordToken.Length >= 30)
                {
                    Preferences.Default.Set("SlackordBotToken", discordToken);
                    DiscordToken = discordToken;
                    await CheckForValidBotToken();
                    if (hasValidBotToken)
                    {
                        WriteToDebugWindow("Slackord received a valid bot token. Bot connection is enabled!\n");
                    }
                    else
                    {
                        WriteToDebugWindow("Slackord received an invalid bot token. Please enter a valid token.\n");
                    }

                }
                else
                {
                    Preferences.Default.Set("SlackordBotToken", string.Empty);
                    await CheckForValidBotToken();
                }
            }
        }

        public async Task ToggleDiscordConnection()
        {
            // Check for valid bot token before proceeding
            if (!hasValidBotToken)
            {
                await ChangeBotConnectionButton("Connect", new Microsoft.Maui.Graphics.Color(128, 128, 128), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                WriteToDebugWindow("Your bot token doesn't look valid. Please enter a new, valid token.");
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
            string currentVersion = Slackord.Classes.Version.GetVersion();
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
                    Button button = MainPage.BotConnectionButtonInstance;
                    button.Text = state;
                    if (textColor != null)
                    {
                        button.TextColor = textColor;
                    }

                    button.BackgroundColor = backgroundColor;
                });
            }
        }

        public static async Task ToggleBotTokenEnable(bool isEnabled, Microsoft.Maui.Graphics.Color color)
        {
            if (MainPage.Current is MainPage mainPage)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MainPage.EnterBotTokenButtonInstance.BackgroundColor = color;
                    MainPage.EnterBotTokenButtonInstance.IsEnabled = isEnabled;
                });
            }
            return;
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
