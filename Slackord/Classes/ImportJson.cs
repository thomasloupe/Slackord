using MenuApp;
using System.Globalization;
using CommunityToolkit.Maui.Storage;

namespace Slackord.Classes
{
    static class ImportJson
    {
        public static readonly Dictionary<string, List<Message>> Channels = new();

        public static async Task ImportJsonFolder(CancellationToken cancellationToken)
        {
            try
            {
                string selectedFolder = null;
                List<string> subDirectories = new();
                List<string> fileList = new();
                int fileCount = 0;
                fileList.Clear();

                var result = await FolderPicker.Default.PickAsync(cancellationToken);
                if (!result.IsSuccessful)
                {
                    return;
                }

                selectedFolder = result.Folder.Path;

                foreach (var file in Directory.EnumerateFiles(selectedFolder, "*.json", SearchOption.AllDirectories))
                {
                    fileCount++;
                }

                foreach (var file in Directory.EnumerateFiles(selectedFolder, "*.json", SearchOption.AllDirectories))
                {
                    var folderName = Path.GetFileName(Path.GetDirectoryName(file));

                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (DateTime.TryParseExact(fileName, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fileDate))
                    {
                        fileList.Add(file);
                    }
                    if (fileList.Count > 0)
                    {
                        var parser = new Parser();
                        await parser.ParseJsonFiles(fileList, folderName, Channels);
                    }
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await MainPage.UpdateParsingMessageProgress(fileList.Count, fileCount);
                    });
                }
                MainPage.PushDebugText();
            }
            catch (Exception ex)
            {
                MainPage.WriteToDebugWindow($"\n\n{ex.Message}\n\n");
            }
        }
    }
}
