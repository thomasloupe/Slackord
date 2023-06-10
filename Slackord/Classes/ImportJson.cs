using MenuApp;
using System.Globalization;
using CommunityToolkit.Maui.Storage;

namespace Slackord.Classes
{
    static class ImportJson
    {
        public static readonly Dictionary<string, List<string>> Channels = new();

        public static async Task ImportJsonFolder(CancellationToken cancellationToken)
        {
            string selectedFolder = null;
            List<string> subDirectories = new();
            try
            {
                var result = await FolderPicker.Default.PickAsync(cancellationToken);
                if (!result.IsSuccessful)
                {
                    return;
                }

                selectedFolder = result.Folder.Path;
                int fileCount = 0;
                List<string> fileList = new();

                foreach (var file in Directory.EnumerateFiles(selectedFolder, "*.json", SearchOption.AllDirectories))
                {
                    fileCount++;
                    var folderName = Path.GetFileName(Path.GetDirectoryName(file));

                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (DateTime.TryParseExact(fileName, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fileDate))
                    {
                        fileList.Add(file);
                    }
                    if (fileList.Count > 0)
                    {
                        Channels[folderName] = fileList;

                        var parser = new Parser();
                        await parser.ParseJsonFiles(fileList, folderName, Channels);
                    }
                }
                int folderCount = Channels.Keys.Count;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await MainPage.Current.DisplayAlert("Information", $"Found {fileCount} JSON files in {folderCount} folders.", "OK");
                });
                MainPage.PushDebugText();
            }
            catch (Exception ex)
            {
                MainPage.WriteToDebugWindow($"\n\n{ex.Message}\n\n");
            }
        }
    }
}
