using Discord;
using Slackord.Classes;

namespace Slackord.Pages
{
    public partial class OptionsPage : ContentPage
    {
        private bool _isPasswordVisible = false;

        public OptionsPage()
        {
            InitializeComponent();

            // Load settings from preferences
            LoadSettings();

            // Check bot connection status and update UI
            UpdateBotTokenEditability();
        }

        private void LoadSettings()
        {
            // Load User Format setting
            string userFormatValue = Preferences.Default.Get("UserFormatValue", ApplicationWindow.CurrentUserFormatOrder.ToString());
            int userFormatIndex = (int)Enum.Parse<ApplicationWindow.UserFormatOrder>(userFormatValue);
            UserFormatPicker.SelectedIndex = userFormatIndex;

            // Load Timestamp Format setting
            string timestampValue = Preferences.Default.Get("TimestampValue", "12 Hour");
            TimestampFormatPicker.SelectedIndex = timestampValue == "12 Hour" ? 0 : 1;

            // Load Bot Token
            BotTokenEntry.Text = Preferences.Default.Get("SlackordBotToken", string.Empty);

            // Load Check for Updates setting (default to true if not set)
            bool checkForUpdates = Preferences.Default.Get("CheckForUpdatesOnStartup", true);
            CheckUpdatesSwitch.IsToggled = checkForUpdates;
        }

        // Removed unused parameter from method signature
        public void OnBotConnectionStateChanged()
        {
            UpdateBotTokenEditability();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateBotTokenEditability();
        }

        private void ToggleTokenVisibility_Clicked(object sender, EventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            BotTokenEntry.IsPassword = !_isPasswordVisible;
            ((Button)sender).Text = _isPasswordVisible ? "Hide" : "Reveal";
        }

        private async void SaveButton_Clicked(object sender, EventArgs e)
        {
            bool tokenChanged = false;
            string currentToken = Preferences.Default.Get("SlackordBotToken", string.Empty);

            // Save User Format
            if (UserFormatPicker.SelectedIndex >= 0)
            {
                var selectedUserFormat = (ApplicationWindow.UserFormatOrder)UserFormatPicker.SelectedIndex;
                Preferences.Default.Set("UserFormatValue", selectedUserFormat.ToString());
                ApplicationWindow.CurrentUserFormatOrder = selectedUserFormat;
            }

            // Save Timestamp Format
            if (TimestampFormatPicker.SelectedIndex >= 0)
            {
                string timestampValue = TimestampFormatPicker.SelectedIndex == 0 ? "12 Hour" : "24 Hour";
                Preferences.Default.Set("TimestampValue", timestampValue);
            }

            // Save and validate Bot Token
            if (BotTokenEntry.Text?.Trim() != currentToken)
            {
                tokenChanged = true;

                if (!string.IsNullOrEmpty(BotTokenEntry.Text))
                {
                    string token = BotTokenEntry.Text.Trim();
                    if (ValidateBotToken(token))
                    {
                        Preferences.Default.Set("SlackordBotToken", token);
                        // Token is valid
                    }
                    else
                    {
                        // Show warning about invalid token, but still save it
                        await DisplayAlert("Warning", "The bot token you entered appears to be invalid. A valid Discord bot token is typically at least 30 characters long.", "OK");
                        Preferences.Default.Set("SlackordBotToken", token);
                    }
                }
                else
                {
                    // Clear the token if field is empty
                    Preferences.Default.Set("SlackordBotToken", string.Empty);
                }
            }

            // Save Check for Updates setting
            Preferences.Default.Set("CheckForUpdatesOnStartup", CheckUpdatesSwitch.IsToggled);

            // Update UI components that display these settings
            await UpdateMainPageUISettings();

            // If token changed, refresh the bot connection status
            if (tokenChanged)
            {
                // Create a new instance to check the token validity
                await new ApplicationWindow().CheckForValidBotToken();
            }

            // Show confirmation
            await DisplayAlert("Success", "Settings saved successfully!", "OK");

            // Close the options page
            await Navigation.PopAsync();
        }

        private async void ResetButton_Clicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Confirm Reset",
                "Are you sure you want to reset all settings to default values?",
                "Yes", "No");

            if (confirm)
            {
                // Reset to default values
                UserFormatPicker.SelectedIndex = 0; // DisplayName_User_RealName
                TimestampFormatPicker.SelectedIndex = 0; // 12 Hour
                BotTokenEntry.Text = string.Empty;
                CheckUpdatesSwitch.IsToggled = true;

                await DisplayAlert("Reset Complete", "All settings have been reset to default values.", "OK");
            }
        }

        private async void CancelButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // Add the handler for About button
        private void About_Clicked(object sender, EventArgs e)
        {
            ApplicationWindow.DisplayAbout();
        }

        // Add the handler for Donate button
        private async void Donate_Clicked(object sender, EventArgs e)
        {
            await ApplicationWindow.CreateDonateAlert();
        }

        public void UpdateBotTokenEditability()
        {
            // Get the current bot connection state
            ConnectionState connectionState = DiscordBot.Instance.GetClientConnectionState();

            // Disable token editing if the bot is connected
            bool isDisconnected = connectionState == ConnectionState.Disconnected;
            BotTokenEntry.IsEnabled = isDisconnected;

            // Update UI to show why it's disabled
            if (!isDisconnected)
            {
                // Add a label or some visual indication that token can't be edited while connected
                BotTokenEntry.Placeholder = "Disconnect bot before changing token";
            }
            else
            {
                BotTokenEntry.Placeholder = "Enter your Discord bot token";
            }
        }

        // This method is marked static to address the CA1822 warning
        private static async Task UpdateMainPageUISettings()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Update the user format settings
                await ApplicationWindow.GetUserFormatValue();

                // Update the timestamp settings
                await ApplicationWindow.GetTimeStampValue();

                // Since we can't directly access applicationWindow, create a new instance for token check
                await new ApplicationWindow().CheckForValidBotToken();
            });
        }

        private static bool ValidateBotToken(string token)
        {
            // Simple validation: Token should be at least 30 characters
            return !string.IsNullOrEmpty(token) && token.Trim().Length >= 30;
        }

        private void OnLabelClicked(object sender, TappedEventArgs e)
        {
            try
            {
                var uri = new Uri("https://discord.gg/yccMweYPN8");
                Launcher.Default.OpenAsync(uri);
            }
            catch (Exception ex)
            {
                Application.Current.MainPage.DisplayAlert("Error", $"Unable to open link: {ex.Message}", "OK");
            }
        }
    }
}