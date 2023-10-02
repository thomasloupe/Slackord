using MenuApp;
using Octokit;

namespace Slackord.Classes
{
    class UpdateCheck
    {
        private static GitHubClient _octoClient;

        public static async Task CheckForUpdates()
        {
            string _currentVersion = Version.GetVersion();

            _octoClient = new GitHubClient(new ProductHeaderValue("Slackord"));

            var releases = await _octoClient.Repository.Release.GetAll("thomasloupe", "Slackord");
            var latest = releases[0];
            if (_currentVersion == latest.TagName)
            {
                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(_currentVersion, "You have the latest version, " + _currentVersion + "!", "OK");
            }
            else if (_currentVersion != latest.TagName)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage.DebugWindowInstance.Text += $"""

                Your Slackord version is out of date.
                Current Version: {_currentVersion}
                Latest Version: {latest.TagName}
                Please consider upgrading at https://github.com/thomasloupe/Slackord/release/{latest.TagName}.

                """;
                });

                var result = await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("Update Available!",
                    "A new version of Slackord is available!" + "\n" +
                    $"Current version: {_currentVersion}" + "\n" +
                    $"Latest version: {latest.TagName}" + "\n" +
                    "Would you like to visit the download page?",
                    "Yes", "No");

                if (result)
                {
                    var url = $"https://github.com/thomasloupe/Slackord/releases/tag/{latest.TagName}";
                    await Launcher.OpenAsync(new Uri(url));
                }
            }

            await Task.CompletedTask;
        }
    }
}
