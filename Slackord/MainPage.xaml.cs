using Discord;
using Slackord.Classes;
using System;
using System.Text;

namespace MenuApp
{
    public partial class MainPage : ContentPage
    {
        //TODO: private
        public static MainPage Current { get; private set; }
        public static Editor DebugWindowInstance { get; set; }
        private CancellationTokenSource cancellationTokenSource;
        public static ProgressBar ProgressBarInstance { get; set; }
        public static Button BotConnectionButtonInstance { get; set; }
        public static Label ProgressBarTextInstance { get; set; }
        public static Button EnterBotTokenButtonInstance { get; set; }
        public string DiscordToken;
        private readonly DiscordBot _discordBot;
        private bool isFirstRun;
        private bool hasValidBotToken;

        public MainPage()
        {
            InitializeComponent();
            if (Current is not null)
            {
                throw new InvalidOperationException("Too many windows");
            }

            DebugWindowInstance = DebugWindow;
            ProgressBarInstance = ProgressBar;
            BotConnectionButtonInstance = BotConnectionButton;
            ProgressBarTextInstance = ProgressBarText;
            EnterBotTokenButtonInstance = EnterBotToken;
            ProgressBarTextInstance.IsVisible = false;
            ProgressBarInstance.IsVisible = false;
            Current = this;
            _discordBot = new DiscordBot();

            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, EventArgs e)
        {
            Initialize();
        }

        private void Initialize()
        {
            CheckForFirstRun();
            CheckForValidBotToken();
        }

        private async void ImportJson_Clicked(object sender, EventArgs e)
        {
            var cts = new CancellationTokenSource();
            Interlocked.Exchange(ref cancellationTokenSource, cts)?.Cancel();
            CancellationToken cancellationToken = cts.Token;
            try
            {
                await Task.Run(() => Slackord.Classes.ImportJson.ImportJsonFolder(cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {

            }
        }

        private void CancelImport_Clicked(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();
        }

        private async void EnterBotToken_Clicked(object sender, EventArgs e)
        {
            await CreateBotTokenPrompt();
        }

        private async void ToggleBotConnection_Clicked(object sender, EventArgs e)
        {
            await ToggleDiscordConnection();
        }

        private async Task ToggleDiscordConnection()
        {
            if (!hasValidBotToken)
            {
                BotConnectionButton.BackgroundColor = new Microsoft.Maui.Graphics.Color(128, 128, 128);
                BotConnectionButton.IsEnabled = false;
                WriteToDebugWindow("Your bot token doesn't look valid. Please enter a new, valid token.");
                return;
            }
            else
            {
                BotConnectionButton.BackgroundColor = new Microsoft.Maui.Graphics.Color(255, 69, 0);
                BotConnectionButton.IsEnabled = true;
                DiscordToken = Preferences.Get("SlackordBotToken", string.Empty).Trim();
            }

            if (_discordBot.DiscordClient == null)
            {
                await ChangeBotConnectionButton("Connecting", new Microsoft.Maui.Graphics.Color(255, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await _discordBot.MainAsync(DiscordToken);
            }
            else if (_discordBot.DiscordClient is { ConnectionState: ConnectionState.Disconnected } disconnectedClient)
            {
                await ChangeBotConnectionButton("Connecting", new Microsoft.Maui.Graphics.Color(255, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await disconnectedClient.StartAsync();
                while (true)
                {
                    var connectionState = disconnectedClient.ConnectionState;
                    if (connectionState == ConnectionState.Connected)
                    {
                        await ChangeBotConnectionButton("Connected", new Microsoft.Maui.Graphics.Color(0, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                        break;
                    }
                }
            }
            else if (_discordBot.DiscordClient.ConnectionState == ConnectionState.Connected)
            {
                await ChangeBotConnectionButton("Disconnecting", new Microsoft.Maui.Graphics.Color(255, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await _discordBot.DiscordClient.StopAsync();
                await ChangeBotConnectionButton("Disconnected", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
            }
        }
        private void CheckForUpdates_Clicked(object sender, EventArgs e)
        {
            _ = CheckForNewVersion();
        }

        private static async Task CheckForNewVersion()
        {
            await UpdateCheck.CheckForUpdates();
        }

        private void About_Clicked(object sender, EventArgs e)
        {
            string currentVersion = Slackord.Classes.Version.GetVersion();
            DisplayAlert("", $"""
Slackord {currentVersion}.
Created by Thomas Loupe.
Github: https://github.com/thomasloupe
Twitter: https://twitter.com/acid_rain
Website: https://thomasloupe.com
""", "OK");
        }

        private void Donate_Clicked(object sender, EventArgs e)
        {
            _ = CreateDonateAlert();
        }

        private async Task CreateDonateAlert()
        {
            var url = "https://paypal.me/thomasloupe";
            var message = """
Slackord will always be free!
If you'd like to buy me a beer anyway, I won't tell you not to!
Would you like to open the donation page now?
""";

            var result = await DisplayAlert("Slackord is free, but beer is not!", message, "Yes", "No");

            if (result)
            {
                await Launcher.OpenAsync(new Uri(url));
            }
        }

        private async void Exit_Clicked(object sender, EventArgs e)
        {
            await ExitApplication();
        }

        private async Task ExitApplication()
        {
            var result = await DisplayAlert("Confirm Exit", "Are you sure you want to quit Slackord?", "Yes", "No");

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

        private void CopyLog_Clicked(object sender, EventArgs e)
        {
            DebugWindow.Focus();
            DebugWindow.CursorPosition = 0;
            DebugWindow.SelectionLength = DebugWindow.Text.Length;

            string selectedText = DebugWindow.Text;

            if (!string.IsNullOrEmpty(selectedText))
            {
                Clipboard.SetTextAsync(selectedText);
            }
        }

        private void ClearLog_Clicked(object sender, EventArgs e)
        {
            DebugWindow.Text = string.Empty;
        }

        private async Task CreateBotTokenPrompt()
        {
            string discordToken = await DisplayPromptAsync("Enter Bot Token", "Please enter your bot's token:", "OK", "Cancel", maxLength: 90);

            if (!string.IsNullOrEmpty(discordToken))
            {
                if (discordToken.Length >= 30)
                {
                    Preferences.Default.Set("SlackordBotToken", discordToken);
                    DiscordToken = discordToken;
                    CheckForValidBotToken();
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
                    CheckForValidBotToken();
                }
            }
        }

        private static readonly StringBuilder debugOutput = new();
        public static void WriteToDebugWindow(string text)
        {
            debugOutput.Append(text);
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

            DebugWindowInstance.Text += debugOutput.ToString();
            debugOutput.Clear();
        }

        private void CheckForFirstRun()
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
                    DebugWindow.Text += "Welcome back to Slackord!\n";
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


        private void CheckForValidBotToken()
        {
            if (isFirstRun)
            {
                if (Preferences.Default.ContainsKey("SlackordBotToken"))
                {
                    DiscordToken = Preferences.Default.Get("SlackordBotToken", string.Empty).Trim();
                    if (DiscordToken.Length > 30)
                    {
                        BotConnectionButton.BackgroundColor = new Microsoft.Maui.Graphics.Color(255, 69, 0);
                        BotConnectionButton.IsEnabled = true;
                        hasValidBotToken = true;
                        WriteToDebugWindow("Slackord found an existing valid bot token, and will use it.\n");
                    }
                    else
                    {
                        BotConnectionButton.BackgroundColor = new Microsoft.Maui.Graphics.Color(128, 128, 128);
                        BotConnectionButton.IsEnabled = false;
                        hasValidBotToken = false;
                        WriteToDebugWindow("Slackord tried to load your last token, but wasn't successful. Please re-enter a new, valid token.\n");
                    }
                }
                else
                {
                    Preferences.Default.Set("SlackordBotToken", string.Empty);
                    BotConnectionButton.BackgroundColor = new Microsoft.Maui.Graphics.Color(128, 128, 128);
                    BotConnectionButton.IsEnabled = false;
                    hasValidBotToken = false;
                    WriteToDebugWindow("Please enter a valid bot token to enable bot connection.");
                }
            }
            else
            {
                DiscordToken = Preferences.Default.Get("SlackordBotToken", string.Empty).Trim();
                if (DiscordToken.Length > 30)
                {
                    BotConnectionButton.BackgroundColor = new Microsoft.Maui.Graphics.Color(255, 69, 0);
                    BotConnectionButton.IsEnabled = true;
                    hasValidBotToken = true;
                    WriteToDebugWindow("Slackord found an existing valid bot token, and will use it.\n");
                }
                else
                {
                    BotConnectionButton.BackgroundColor = new Microsoft.Maui.Graphics.Color(128, 128, 128);
                    BotConnectionButton.IsEnabled = false;
                    hasValidBotToken = false;
                    WriteToDebugWindow("Slackord tried to load your last token, but wasn't successful. Please re-enter a new, valid token.\n");
                }
            }
        }

        public static async Task ChangeBotConnectionButton(string state, Microsoft.Maui.Graphics.Color backgroundColor, Microsoft.Maui.Graphics.Color textColor)
        {
            BotConnectionButtonInstance.BackgroundColor = backgroundColor;

            if (Current is MainPage mainPage)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Button button = mainPage.BotConnectionButton;
                    button.Text = state;
                    if (textColor != null)
                    {
                        button.TextColor = textColor;
                    }
                });
            }
        }

        public static async Task ToggleBotTokenEnable(bool isEnabled, Microsoft.Maui.Graphics.Color color)
        {
            if (Current is MainPage mainPage)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    EnterBotTokenButtonInstance.BackgroundColor = color;
                    EnterBotTokenButtonInstance.IsEnabled = isEnabled;
                });
            }
            return;
        }

        public static async Task UpdateMessageSendProgress(float progress, float totalMessagesToSend)
        {
            if (!MainThread.IsMainThread)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await UpdateMessageSendProgress(progress, totalMessagesToSend);
                });
                return;
            }

            Current.ProgressBar.IsVisible = true;
            Current.ProgressBarText.IsVisible = true;
            var currentProgress = progress / totalMessagesToSend;
            Current.ProgressBarText.Text = $"{progress} of {totalMessagesToSend} messages sent.";
            Current.ProgressBar.Progress = currentProgress;
        }

        public static async Task UpdateParsingMessageProgress(float currenFileCount, float totalFileCount)
        {
            if (!MainThread.IsMainThread)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await UpdateParsingMessageProgress(currenFileCount, totalFileCount);
                });
                return;
            }

            Current.ProgressBar.IsVisible = true;
            Current.ProgressBarText.IsVisible = true;
            var currentProgress = currenFileCount / totalFileCount;
            Current.ProgressBarText.Text = $"{currenFileCount} of {totalFileCount} files parsed.";
            Current.ProgressBar.Progress = currentProgress;
        }
    }
}