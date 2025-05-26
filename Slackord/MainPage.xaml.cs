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
        private readonly ApplicationWindow applicationWindow;
        public static Label ProcessingStateLabelInstance { get; set; }
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
            ProcessingStateLabelInstance = ProcessingStateLabel;

            ProgressBarTextInstance.IsVisible = false;
            ProgressBarInstance.IsVisible = false;
            Current = this;
            applicationWindow = new ApplicationWindow();

            // Subscribe to processing state changes
            ProcessingManager.Instance.StateChanged += OnProcessingStateChanged;

            Loaded += MainPage_Loaded;
        }
        private void MainPage_Loaded(object sender, EventArgs e)
        {
            _ = Initialize().ConfigureAwait(false);
        }
        private async Task Initialize()
        {
            // First run check
            ApplicationWindow.CheckForFirstRun();

            // Only check for updates if the setting is enabled
            bool checkForUpdatesOnStartup = Preferences.Default.Get("CheckForUpdatesOnStartup", true);
            if (checkForUpdatesOnStartup)
            {
                await ApplicationWindow.CheckForNewVersion(true);
            }

            // Check for valid bot token
            await applicationWindow.CheckForValidBotToken();

            // Get application settings (but don't duplicate output)
            await ApplicationWindow.GetTimeStampValue();
            await ApplicationWindow.GetUserFormatValue();

            // Check for partial imports - use the static method directly
            await ApplicationWindow.CheckForPartialImport();

            ProcessingManager.Instance.StateChanged += (sender, state) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ProcessingStateLabelInstance.Text = state.ToString().Replace("_", " ");
                    ProcessingStateLabelInstance.TextColor = state switch
                    {
                        ProcessingState.ReadyForDiscordImport => Colors.Green,
                        ProcessingState.Error => Colors.Red,
                        ProcessingState.Completed => Colors.Blue,
                        _ => Colors.Orange
                    };
                });
            };
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

        private void ToggleBotConnection_Clicked(object sender, EventArgs e)
        {
            _ = applicationWindow.ToggleDiscordConnection().ConfigureAwait(false);
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
            // Reset the current session (if any)
            ImportJson.SetCurrentSession(null);
            ImportJson.TotalHiddenFileCount = 0;
            ApplicationWindow.ResetProgressBar();
        }

        private async void Settings_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Slackord.Pages.OptionsPage());
        }

        private void OnProcessingStateChanged(object sender, ProcessingState newState)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Update the status label with user-friendly text
                ProcessingStateLabelInstance.Text = ProcessingManager.GetDisplayText(newState);

                // Update text color based on state
                ProcessingStateLabelInstance.TextColor = newState switch
                {
                    ProcessingState.ReadyForDiscordImport => Colors.Green,
                    ProcessingState.Completed => Colors.Blue,
                    ProcessingState.Error => Colors.Red,
                    ProcessingState.Idle => Colors.DodgerBlue,
                    _ => Colors.Orange
                };
            });
        }
    }
}