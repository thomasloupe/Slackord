using MenuApp;
using System.Globalization;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Threading;

namespace Slackord
{
    class ImportJson
    {
        public static readonly Dictionary<string, List<string>> Channels = new();

        public static async Task ImportJsonFolder(CancellationToken cancellationToken)
        {
            string selectedFolder = null;
            List<string> subDirectories = new();

            var result = await FolderPicker.Default.PickAsync(cancellationToken);
            if (result.IsSuccessful)
            {
                await Toast.Make($"The folder was picked: Name - {result.Folder.Name}, Path - {result.Folder.Path}", ToastDuration.Long).Show(cancellationToken);
            }
            else
            {
                await Toast.Make($"The folder was not picked with error: {result.Exception.Message}").Show(cancellationToken);
            }

            if (result != null)
                selectedFolder = result.Folder.Path;
            subDirectories = Directory.GetDirectories(selectedFolder).ToList();

            int fileCount = 0;
            int folderCount = subDirectories.Count;

            foreach (var subDir in subDirectories)
            {
                var folderName = Path.GetFileName(subDir);
                var files = Directory.EnumerateFiles(subDir, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(s => s.EndsWith(".JSON", StringComparison.OrdinalIgnoreCase));

                // Create a list to store the files for the channel
                List<string> fileList = new();

                foreach (var file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (DateTime.TryParseExact(fileName, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fileDate))
                    {
                        fileList.Add(file);
                        fileCount++;
                    }
                }

                if (fileList.Count > 0)
                {
                    // Add the channel and its file list to the channels dictionary
                    Channels[folderName] = fileList;

                    // Parse JSON files for the channel
                    var parser = new Parser();
                    await parser.ParseJsonFiles(fileList, folderName, Channels);
                }
            }
            await MainPage.Current.DisplayAlert("Information", $"Found {fileCount} JSON files in {folderCount} folders.", "OK");
        }
    }
}
