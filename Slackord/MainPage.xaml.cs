using Slackord;

namespace MenuApp
{
    public partial class MainPage : ContentPage
    {
        public static MainPage Current { get; private set; }
        public static Editor DebugWindowInstance { get; set; }
        private CancellationTokenSource cancellationTokenSource;
        public static ProgressBar ProgressBarInstance { get; set; }
        public static Button EnterBotTokenButtonInstance { get; set; }
        public string DiscordToken;
        private int totalMessageCount;
        private DiscordBot discordBot;
        private readonly bool hasBotKey;
        private readonly bool isFirstRun;

        public MainPage()
        {
            InitializeComponent();
            DebugWindowInstance = DebugWindow;
            ProgressBarInstance = ProgressBar;
            EnterBotTokenButtonInstance = EnterBotToken;
            Current = this;
            discordBot = new DiscordBot();
            hasBotKey = Preferences.Default.ContainsKey("SlackordBotToken");
            isFirstRun = !Preferences.ContainsKey("FirstRun");
            if (isFirstRun)
            {
                DebugWindow.Text += "Welcome to Slackord!" + "\n";
                Preferences.Default.Set("FirstRun", true);
                Preferences.Default.Set("SlackordBotToken", string.Empty);
            }
            CheckForExistingBotToken();
        }

        private void ImportJson_Clicked(object sender, EventArgs e)
        {
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            _ = Slackord.ImportJson.ImportJsonFolder(cancellationToken);
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
            if (hasBotKey && Preferences.Get("SlackordBotToken", string.Empty) == string.Empty)
            {
                await CreateBotTokenPrompt();
                return;
            }
            else
            {
                DiscordToken = Preferences.Get("SlackordBotToken", string.Empty).Trim();
            }
            if (discordBot._discordClient == null)
            {
                discordBot = new DiscordBot();
                await ChangeBotConnectionButton("Connecting");
                await discordBot.MainAsync(DiscordToken);
            }
            else if (discordBot._discordClient != null && discordBot._discordClient.ConnectionState == Discord.ConnectionState.Disconnected)
            {
                await ChangeBotConnectionButton("Connecting");
                await discordBot.MainAsync(DiscordToken);
            }
            else if (discordBot._discordClient.ConnectionState == Discord.ConnectionState.Connected)
            {
                await ChangeBotConnectionButton("Disconnecting");
                await discordBot._discordClient.LogoutAsync();
                await ChangeBotConnectionButton("Disconnected");
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
            string currentVersion = Slackord.Version.GetVersion();
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
                if (discordToken.Length > 30)
                {
                    DiscordToken = discordToken;
                    Preferences.Set("SlackordBotToken", discordToken);
                }
                else
                {
                    DebugWindow.Text += "Your token wasn't long enough or valid, please re-enter a valid token.\n";
                    Preferences.Set("SlackordBotToken", string.Empty);
                    return;
                }
            }
        }

        public static async Task WriteToDebugWindow(string text)
        {
            MainThread.BeginInvokeOnMainThread(() => 
            {
                DebugWindowInstance.Text += text;
            });
            await Task.CompletedTask;
        }

        private void DebugWindow_TextChanged(object sender, TextChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DebugWindow.CursorPosition = DebugWindow.Text.Length;
            });
        }

        public int TotalMessageCount
        {
            get { return totalMessageCount; }
            set { OnPropertyChanged(); }
        }

        private void CheckForExistingBotToken()
        {
            if (hasBotKey)
            {
                DebugWindow.Text += "Welcome back to Slackord!\n";
                DiscordToken = Preferences.Get("SlackordBotToken", string.Empty).Trim();
            }
            if (string.IsNullOrEmpty(DiscordToken) || string.IsNullOrEmpty(Preferences.Get("SlackordBotToken", string.Empty)))
            {
                BotConnectionButton.Text = "Disabled";
                BotConnectionButton.IsEnabled = false;
                DebugWindow.Text += @"
Slackord tried to load your last bot token but wasn't successful.
The token is not long enough or the token value is empty. Please enter a new token!
";
            }
            else
            {
                DebugWindow.Text += "Slackord 2 found an existing bot token and will use it! Bot connection is now enabled." + "\n";
                BotConnectionButton.Text = "Connect";
                BotConnectionButton.IsEnabled = true;
            }
        }

        public static async Task ChangeBotConnectionButton(string state)
        {
            Page currentPage = Current;

            if (currentPage is MainPage mainPage)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Button button = mainPage.BotConnectionButton;
                    if (state == "Disabled")
                    {
                        button.BackgroundColor = new Color(128, 128, 128);
                    }
                    else
                    {
                        button.BackgroundColor = new Color(255, 69, 0);
                    }
                    button.Text = state;
                });
            }
            await Task.CompletedTask;
        }

        public static async Task ToggleBotTokenEnable(bool isEnabled, Color color)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                EnterBotTokenButtonInstance.BackgroundColor = color;
                EnterBotTokenButtonInstance.IsEnabled = isEnabled;
            });
            await Task.CompletedTask;
        }
    }
}