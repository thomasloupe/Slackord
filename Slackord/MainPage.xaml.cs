using Microsoft.Maui.Controls;
using Slackord.Classes;

namespace MenuApp
{
    public partial class MainPage : ContentPage
    {
        public static MainPage Current { get; private set; }
        public static Editor DebugWindowInstance { get; set; }
        public static ProgressBar ProgressBarInstance { get; set; }
        public static Button BotConnectionButtonInstance { get; set; }
        public static Label ProgressBarTextInstance { get; set; }
        public static Button EnterBotTokenButtonInstance { get; set; }
        private readonly ApplicationWindow applicationWindow;

        public MainPage()
        {
            InitializeComponent();
            if (Current is not null)
            {
                throw new InvalidOperationException("Too many windows.");
            }

            DebugWindowInstance = DebugWindow;
            ProgressBarInstance = ProgressBar;
            BotConnectionButtonInstance = BotConnectionButton;
            ProgressBarTextInstance = ProgressBarText;
            EnterBotTokenButtonInstance = EnterBotToken;
            ProgressBarTextInstance.IsVisible = false;
            ProgressBarInstance.IsVisible = false;
            Current = this;
            applicationWindow = new ApplicationWindow();

            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, EventArgs e)
        {
            Initialize().ConfigureAwait(false);
        }

        private async Task Initialize()
        {
            applicationWindow.CheckForFirstRun();
            await applicationWindow.CheckForValidBotToken();
        }

        private void ImportJson_Clicked(object sender, EventArgs e)
        {
            applicationWindow.ImportJsonAsync().ConfigureAwait(false);
        }

        private void EnterBotToken_Clicked(object sender, EventArgs e)
        {
            applicationWindow.CreateBotTokenPrompt().ConfigureAwait(false);
        }

        private void ToggleBotConnection_Clicked(object sender, EventArgs e)
        {
            applicationWindow.ToggleDiscordConnection().ConfigureAwait(false);
        }
        private void CheckForUpdates_Clicked(object sender, EventArgs e)
        {
            ApplicationWindow.CheckForNewVersion().ConfigureAwait(true);
        }

        private void About_Clicked(object sender, EventArgs e)
        {
            ApplicationWindow.DisplayAbout();
        }

        private void Donate_Clicked(object sender, EventArgs e)
        {
            ApplicationWindow.CreateDonateAlert().ConfigureAwait(false);
        }

        private void Exit_Clicked(object sender, EventArgs e)
        {
            ApplicationWindow.ExitApplication().ConfigureAwait(false);
        }

        private void CopyLog_Clicked(object sender, EventArgs e)
        {
            ApplicationWindow.CopyLog();
        }

        private void ClearLog_Clicked(object sender, EventArgs e)
        {
            ApplicationWindow.ClearLog();
        }
    }
}
