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
                //subDirectories = Directory.GetDirectories(selectedFolder).ToList();

                //int folderCount = subDirectories.Count;
                int fileCount = 0;

                //foreach (var subDir in subDirectories)
                //{
                //    var folderName = Path.GetFileName(subDir);
                //    var files = Directory.EnumerateFiles(subDir, "*.*", SearchOption.TopDirectoryOnly)
                //        .Where(s => s.EndsWith(".JSON", StringComparison.OrdinalIgnoreCase));

                //    fileCount += files.Count();
                //}
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
                        // Add the channel and its file list to the channels dictionary
                        Channels[folderName] = fileList;

                        // Parse JSON files for the channel
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

                //foreach (var subDir in subDirectories)
                //{
                //    var folderName = Path.GetFileName(subDir);
                //    var files = Directory.EnumerateFiles(subDir, "*.*", SearchOption.TopDirectoryOnly)
                //        .Where(s => s.EndsWith(".JSON", StringComparison.OrdinalIgnoreCase));
                //
                //    // Create a list to store the files for the channel
                //    List<string> fileList = new();
                //
                //    foreach (var file in files)
                //    {
                //        string fileName = Path.GetFileNameWithoutExtension(file);
                //        if (DateTime.TryParseExact(fileName, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fileDate))
                //        {
                //            fileList.Add(file);
                //        }
                //    }
                //
                //    if (fileList.Count > 0)
                //    {
                //        // Add the channel and its file list to the channels dictionary
                //        Channels[folderName] = fileList;
                //
                //        // Parse JSON files for the channel
                //        var parser = new Parser();
                //        await parser.ParseJsonFiles(fileList, folderName, Channels);
                //    }
                //}
            }
            catch (Exception ex)
            {
                //writeLog($"\n\n{ex.Message}\n\n");
                MainPage.WriteToDebugWindow($"\n\n{ex.Message}\n\n");
                //TODO
                //MainPage.DisplayAlert
                Page page = new();
                await page.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
