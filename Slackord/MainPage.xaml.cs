﻿using Slackord.Classes;

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
        public static Button TimeStampButtonInstance { get; set; }
        public static Button UserFormatButtonInstance { get; set; }
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
            TimeStampButtonInstance = TimestampToggle;
            UserFormatButtonInstance = UserFormatToggle;
            ProgressBarTextInstance.IsVisible = false;
            ProgressBarInstance.IsVisible = false;
            Current = this;
            applicationWindow = new ApplicationWindow();

            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, EventArgs e)
        {
            _ = Initialize().ConfigureAwait(false);
        }

        private async Task Initialize()
        {
            applicationWindow.CheckForFirstRun();
            await ApplicationWindow.CheckForNewVersion(true);
            await applicationWindow.CheckForValidBotToken();
            await ApplicationWindow.GetTimeStampValue();
            await ApplicationWindow.GetUserFormatValue();
        }

        private void ImportServer_Clicked(object sender, EventArgs e)
        {
            ResetSlackordImportedData();
            _ = applicationWindow.ImportJsonAsync(true);
        }

        private void ImportChannel_Clicked(object sender, EventArgs e)
        {
            ResetSlackordImportedData();
            _ = applicationWindow.ImportJsonAsync(false);
        }

        private void EnterBotToken_Clicked(object sender, EventArgs e)
        {
            _ = applicationWindow.CreateBotTokenPrompt().ConfigureAwait(false);
        }

        private void ToggleBotConnection_Clicked(object sender, EventArgs e)
        {
            _ = applicationWindow.ToggleDiscordConnection().ConfigureAwait(false);
        }

        private void Timestamp_Clicked(object sender, EventArgs e)
        {
            _ = ApplicationWindow.SetTimestampValue().ConfigureAwait(false);
        }

        private void UserFormat_Clicked(object sender, EventArgs e)
        {
            _ = ApplicationWindow.SetUserFormatValue().ConfigureAwait(false);
        }

        private void CheckForUpdates_Clicked(object sender, EventArgs e)
        {
            _ = ApplicationWindow.CheckForNewVersion(false).ConfigureAwait(true);
        }

        private void About_Clicked(object sender, EventArgs e)
        {
            ApplicationWindow.DisplayAbout();
        }

        private void Donate_Clicked(object sender, EventArgs e)
        {
            _ = ApplicationWindow.CreateDonateAlert().ConfigureAwait(false);
        }

        private void Exit_Clicked(object sender, EventArgs e)
        {
            _ = ApplicationWindow.ExitApplication().ConfigureAwait(false);
        }

        private void CopyLog_Clicked(object sender, EventArgs e)
        {
            ApplicationWindow.CopyLog();
        }

        private void ClearLog_Clicked(object sender, EventArgs e)
        {
            ApplicationWindow.ClearLog();
        }

        private static void ResetSlackordImportedData()
        {
            ImportJson.Channels.Clear();
            ImportJson.TotalHiddenFileCount = 0;
            ApplicationWindow.ResetProgressBar();
        }
    }
}
