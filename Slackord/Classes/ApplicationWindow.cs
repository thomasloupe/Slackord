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

        public async Task ImportJsonAsync()
        {
            using var cts = new CancellationTokenSource();
            Interlocked.Exchange(ref cancellationTokenSource, cts)?.Cancel();
            CancellationToken cancellationToken = cts.Token;
            try
            {
                await ImportJson.ImportJsonAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Handle the cancellation.
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
            if (!hasValidBotToken)
            {
                await ChangeBotConnectionButton("Connect", new Microsoft.Maui.Graphics.Color(128, 128, 128), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                WriteToDebugWindow("Your bot token doesn't look valid. Please enter a new, valid token.");
                return;
            }
            else
            {
                await ChangeBotConnectionButton("Connect", new Microsoft.Maui.Graphics.Color(255, 69, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                DiscordToken = Preferences.Get("SlackordBotToken", string.Empty).Trim();
            }

            if (_discordBot.DiscordClient == null)
            {
                await ChangeBotConnectionButton("Connecting", new Microsoft.Maui.Graphics.Color(255, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await _discordBot.MainAsync(DiscordToken);
            }
            else if (_discordBot.GetClientConnectionState() == ConnectionState.Disconnected)
            {
                await ChangeBotConnectionButton("Connecting", new Microsoft.Maui.Graphics.Color(255, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await _discordBot.StartClientAsync();
                while (true)
                {
                    var connectionState = _discordBot.GetClientConnectionState();
                    if (connectionState == ConnectionState.Connected)
                    {
                        await ChangeBotConnectionButton("Connected", new Microsoft.Maui.Graphics.Color(0, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                        break;
                    }
                }
            }
            else if (_discordBot.GetClientConnectionState() == ConnectionState.Connected)
            {
                await ChangeBotConnectionButton("Disconnecting", new Microsoft.Maui.Graphics.Color(255, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await _discordBot.StopClientAsync();
                await ChangeBotConnectionButton("Disconnected", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
            }
        }

        public static async Task CheckForNewVersion()
        {
            await UpdateCheck.CheckForUpdates();
        }

        public static void DisplayAbout()
        {
            string currentVersion = Slackord.Classes.Version.GetVersion();
            MainPageInstance.DisplayAlert("", $@"
Slackord {currentVersion}.
Created by Thomas Loupe.
Github: https://github.com/thomasloupe
Twitter: https://twitter.com/acid_rain
Website: https://thomasloupe.com
", "OK");
        }

        public static async Task CreateDonateAlert()
        {
            var url = "https://paypal.me/thomasloupe";
            var message = @"
Slackord will always be free!
If you'd like to buy me a beer anyway, I won't tell you not to!
Would you like to open the donation page now?
";

            var result = await MainPageInstance.DisplayAlert("Slackord is free, but beer is not!", message, "Yes", "No");

            if (result)
            {
                await Launcher.OpenAsync(new Uri(url));
            }
        }

        public static async Task ExitApplication()
        {
            var result = await MainPageInstance.DisplayAlert("Confirm Exit", "Are you sure you want to quit Slackord?", "Yes", "No");

            if (result)
            {
                var operatingSystem = DeviceInfo.Platform;
                if (operatingSystem == DevicePlatform.MacCatalyst)
                {
                    System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
                }
                else if (operatingSystem == DevicePlatform.WinUI)
                {
                    Application.Current.Quit();
                }
            }
        }

        public static void CopyLog()
        {
            MainPage.DebugWindowInstance.Focus();
            MainPage.DebugWindowInstance.CursorPosition = 0;
            MainPage.DebugWindowInstance.SelectionLength = MainPage.DebugWindowInstance.Text.Length;

            string selectedText = MainPage.DebugWindowInstance.Text;

            if (!string.IsNullOrEmpty(selectedText))
            {
                Clipboard.SetTextAsync(selectedText);
            }
        }

        public static void ClearLog()
        {
            MainPage.DebugWindowInstance.Text = string.Empty;
        }

        private static readonly StringBuilder debugOutput = new();
        public static void WriteToDebugWindow(string text)
        {
            debugOutput.Append(text);
            PushDebugText();
        }

        public static void PushDebugText()
        {
            if (!MainThread.IsMainThread)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PushDebugText();
                });
                return;
            }

            MainPage.DebugWindowInstance.Text += debugOutput.ToString();
            debugOutput.Clear();
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
                MainThread.InvokeOnMainThreadAsync(() =>
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
                MainThread.InvokeOnMainThreadAsync(() =>
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
                MainThread.InvokeOnMainThreadAsync(() =>
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
                MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MainPage.ProgressBarInstance.Progress = 0;
                    MainPage.ProgressBarTextInstance.Text = string.Empty;
                });
            }
        }
    }
}
