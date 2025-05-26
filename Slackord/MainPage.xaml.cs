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
        public static Label ProcessingStateLabelInstance { get; set; }

        private readonly ApplicationWindow applicationWindow;

        /// <summary>
        /// Initializes the main page and its components
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            if (Current is not null)
            {
                throw new InvalidOperationException("Too many windows.");
            }

            DebugWindowInstance = DebugWindow;
            ProgressBarInstance = ProgressBar;
            BotConnectionButtonInstance = BotConnectionButton;
            ProgressBarTextInstance = ProgressBarText;
            ProcessingStateLabelInstance = ProcessingStateLabel;

            ProgressBarTextInstance.IsVisible = false;
            ProgressBarInstance.IsVisible = false;
            Current = this;
            applicationWindow = new ApplicationWindow();

            ProcessingManager.Instance.StateChanged += OnProcessingStateChanged;

            Loaded += MainPage_Loaded;
        }

        /// <summary>
        /// Handles the Loaded event by initializing the page
        /// </summary>
        private void MainPage_Loaded(object sender, EventArgs e)
        {
            _ = Initialize().ConfigureAwait(false);
        }

        /// <summary>
        /// Performs initial checks and setups for the main page
        /// </summary>
        private async Task Initialize()
        {
            ApplicationWindow.CheckForFirstRun();

            bool checkForUpdatesOnStartup = Preferences.Default.Get("CheckForUpdatesOnStartup", true);
            if (checkForUpdatesOnStartup)
            {
                await ApplicationWindow.CheckForNewVersion(true);
            }

            await applicationWindow.CheckForValidBotToken();
            await ApplicationWindow.GetTimeStampValue();
            await ApplicationWindow.GetUserFormatValue();
            await ApplicationWindow.CheckForPartialImport();
        }

        /// <summary>
        /// Initiates full server import when button is clicked
        /// </summary>
        private void ImportServer_Clicked(object sender, EventArgs e)
        {
            ResetSlackordImportedData();
            _ = applicationWindow.ImportJsonAsync(true);
        }

        /// <summary>
        /// Initiates single channel import when button is clicked
        /// </summary>
        private void ImportChannel_Clicked(object sender, EventArgs e)
        {
            ResetSlackordImportedData();
            _ = applicationWindow.ImportJsonAsync(false);
        }

        /// <summary>
        /// Toggles the bot connection state when button is clicked
        /// </summary>
        private void ToggleBotConnection_Clicked(object sender, EventArgs e)
        {
            _ = applicationWindow.ToggleDiscordConnection().ConfigureAwait(false);
        }

        /// <summary>
        /// Copies debug log content to clipboard
        /// </summary>
        private void CopyLog_Clicked(object sender, EventArgs e)
        {
            ApplicationWindow.CopyLog();
        }

        /// <summary>
        /// Clears the debug log content
        /// </summary>
        private void ClearLog_Clicked(object sender, EventArgs e)
        {
            ApplicationWindow.ClearLog();
        }

        /// <summary>
        /// Resets imported Slackord data
        /// </summary>
        private static void ResetSlackordImportedData()
        {
            ImportJson.SetCurrentSession(null);
            ImportJson.TotalHiddenFileCount = 0;
            ApplicationWindow.ResetProgressBar();
        }

        /// <summary>
        /// Navigates to the settings page
        /// </summary>
        private async void Settings_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Slackord.Pages.OptionsPage());
        }

        /// <summary>
        /// Handles processing state changes by updating the UI
        /// </summary>
        private void OnProcessingStateChanged(object sender, ProcessingState newState)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ProcessingStateLabelInstance.Text = ProcessingManager.GetDisplayText(newState);
                ProcessingStateLabelInstance.TextColor = newState switch
                {
                    ProcessingState.ReadyForDiscordImport => Colors.Green,
                    ProcessingState.Completed => Colors.Green,
                    ProcessingState.Error => Colors.Red,
                    ProcessingState.Idle => Colors.Yellow,
                    _ => Colors.Orange
                };
            });
        }
    }
}
