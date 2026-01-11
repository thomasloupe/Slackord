using MenuApp;
using Octokit;

namespace Slackord.Classes
{
    /// <summary>
    /// Handles checking for application updates from GitHub releases
    /// </summary>
    internal class UpdateCheck
    {
        /// <summary>
        /// GitHub API client for accessing release information
        /// </summary>
        private static GitHubClient _octoClient;

        /// <summary>
        /// Checks for available updates from the GitHub repository
        /// </summary>
        /// <param name="isStartupCheck">Whether this is an automatic startup check or manual check</param>
        public static async Task CheckForUpdates(bool isStartupCheck)
        {
            if (isStartupCheck)
            {
                bool checkForUpdatesOnStartup = Preferences.Default.Get("CheckForUpdatesOnStartup", true);
                if (!checkForUpdatesOnStartup)
                {
                    return;
                }
            }

            string _currentVersion = Version.GetVersion();
            _octoClient = new GitHubClient(new ProductHeaderValue("Slackord"));

            try
            {
                IReadOnlyList<Release> releases = await _octoClient.Repository.Release.GetAll("thomasloupe", "Slackord");
                Release latest = releases[0];

                if (_currentVersion == latest.TagName)
                {
                    if (isStartupCheck)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            MainPage.DebugWindowInstance.Text += $"""
                                Current Version: {_currentVersion}
                                Latest Version: {latest.TagName}
                                You have the latest Slackord version!

                                """;
                        });
                    }
                    else
                    {
                        await Microsoft.Maui.Controls.Application.Current.Windows[0].Page.DisplayAlertAsync(_currentVersion, "You have the latest version, " + _currentVersion + "!", "OK");
                    }
                }
                else if (_currentVersion != latest.TagName)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MainPage.DebugWindowInstance.Text += $"""
                                Your Slackord version is out of date.
                                Current Version: {_currentVersion}
                                Latest Version: {latest.TagName}
                                
                                Release Notes:
                                {latest.Body}
                                Please consider upgrading at https://github.com/thomasloupe/Slackord/release/{latest.TagName}.
                                

                                """;
                    });
                    bool result = await Microsoft.Maui.Controls.Application.Current.Windows[0].Page.DisplayAlertAsync("Slackord Update Available!",
                        $@"A new version of Slackord is available!
    Current version: {_currentVersion}
    Latest version: {latest.TagName}
            
    You are missing the following features and bug fixes:
    {latest.Body}
    It is highly recommended that you always have the latest version available.    
    Would you like to visit the download page?",
                        "Yes", "No");
                    if (result)
                    {
                        string url = $"https://github.com/thomasloupe/Slackord/releases/tag/{latest.TagName}";
                        _ = await Launcher.OpenAsync(new Uri(url));
                    }
                }
            }
            catch (Exception ex)
            {
                if (!isStartupCheck)
                {
                    await Microsoft.Maui.Controls.Application.Current.Windows[0].Page.DisplayAlertAsync(
                        "Update Check Failed",
                        $"Could not check for updates: {ex.Message}",
                        "OK");
                }

                ApplicationWindow.WriteToDebugWindow($"Failed to check for updates: {ex.Message}\n");
            }
        }
    }
}