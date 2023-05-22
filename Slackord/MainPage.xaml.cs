using Discord;
using MauiApp1.Classes;

namespace MenuApp
{
    public partial class MainPage : ContentPage
    {
        public static MainPage Current { get; private set; }
        public static Editor DebugWindowInstance { get; set; }
        private CancellationTokenSource cancellationTokenSource;
        public static ProgressBar ProgressBarInstance { get; set; }
        public static Button BotConnectionButtonInstance { get; set; }
        public static Label ProgressBarTextInstance { get; set; }
        public static Button EnterBotTokenButtonInstance { get; set; }
        public string DiscordToken;
        private DiscordBot discordBot;
        private bool isFirstRun;
        private bool hasValidBotToken;

        public MainPage()
        {
            InitializeComponent();
            DebugWindowInstance = DebugWindow;
            ProgressBarInstance = ProgressBar;
            BotConnectionButtonInstance = BotConnectionButton;
            ProgressBarTextInstance = ProgressBarText;
            EnterBotTokenButtonInstance = EnterBotToken;
            ProgressBarTextInstance.IsVisible = false;
            ProgressBarInstance.IsVisible = false;
            Current = this;
            discordBot = new DiscordBot();

            Initialize();
        }

        private async void Initialize()
        {
            await CheckForFirstRun();
            await CheckForValidBotToken();
        }

        private void ImportJson_Clicked(object sender, EventArgs e)
        {
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            _ = MauiApp1.Classes.ImportJson.ImportJsonFolder(cancellationToken);
        }

        private void CancelImport_Clicked(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();
        }

        private void EnterBotToken_Clicked(object sender, EventArgs e)
        {
            _ = CreateBotTokenPrompt();
        }
        
        private void ToggleBotConnection_Clicked(object sender, EventArgs e)
        {
            _ = ToggleDiscordConnection();
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

            if (discordBot._discordClient == null)
            {
                discordBot = new DiscordBot();
                await ChangeBotConnectionButton("Connecting", new Microsoft.Maui.Graphics.Color(255, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await discordBot.MainAsync(DiscordToken);
            }
            else if (discordBot._discordClient != null && discordBot._discordClient.ConnectionState == ConnectionState.Disconnected)
            {
                await ChangeBotConnectionButton("Connecting", new Microsoft.Maui.Graphics.Color(255, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await discordBot._discordClient.StartAsync();
                while (true)
                {
                    var connectionState = discordBot._discordClient.ConnectionState;
                    if (connectionState.ToString() == "Connected")
                    {
                        await ChangeBotConnectionButton("Connected", new Microsoft.Maui.Graphics.Color(0, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                        break;
                    }
                }
            }
            else if (discordBot._discordClient.ConnectionState == ConnectionState.Connected)
            {
                await ChangeBotConnectionButton("Disconnecting", new Microsoft.Maui.Graphics.Color(255, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await discordBot._discordClient.StopAsync();
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
            await Task.CompletedTask;
        }

        private void About_Clicked(object sender, EventArgs e)
        {
            string currentVersion = MauiApp1.Classes.Version.GetVersion();
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

        private void Exit_Clicked(object sender, EventArgs e)
        {
            _ = ExitApplication();
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
                // OK button was clicked and a token was entered
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

        public static void WriteToDebugWindow(string text)
        {
            DebugWindowInstance.Text += text;
        }

        private async Task CheckForFirstRun()
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
            await Task.CompletedTask;
        }


        private async Task CheckForValidBotToken()
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
                if (DiscordToken .Length > 30)
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
            await Task.CompletedTask;
        }

        public static async Task ChangeBotConnectionButton(string state, Microsoft.Maui.Graphics.Color backgroundColor, Microsoft.Maui.Graphics.Color textColor)
        {
            Page currentPage = Current;
            BotConnectionButtonInstance.BackgroundColor = backgroundColor;

            if (currentPage is MainPage mainPage)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Button button = mainPage.BotConnectionButton;
                    button.Text = state;
                    if (textColor != null)
                    {
                        button.TextColor = textColor;
                    }
                });
            }
            await Task.CompletedTask;
        }

        public static async Task ToggleBotTokenEnable(bool isEnabled, Microsoft.Maui.Graphics.Color color)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                EnterBotTokenButtonInstance.BackgroundColor = color;
                EnterBotTokenButtonInstance.IsEnabled = isEnabled;
            });
            await Task.CompletedTask;
        }

        public static async Task CommitProgress(float progress, float totalMessagesToSend)
        {
            ProgressBarInstance.IsVisible = true;
            ProgressBarTextInstance.IsVisible = true;
            var currentProgress = progress / totalMessagesToSend;
            ProgressBarTextInstance.Text = $"{progress} of {totalMessagesToSend} messages sent.";
            ProgressBarInstance.Progress = currentProgress;
            await Task.CompletedTask;
        }
    }
}