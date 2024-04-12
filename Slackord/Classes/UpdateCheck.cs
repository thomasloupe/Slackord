using MenuApp;
using Octokit;

namespace Slackord.Classes
{
    internal class UpdateCheck
    {
        private static GitHubClient _octoClient;

        public static async Task CheckForUpdates(bool isStartupCheck)
        {
            string _currentVersion = Version.GetVersion();
            _octoClient = new GitHubClient(new ProductHeaderValue("Slackord"));
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
                    await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(_currentVersion, "You have the latest version, " + _currentVersion + "!", "OK");
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

                bool result = await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("Slackord Update Available!",
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

            await Task.CompletedTask;
        }
    }
}