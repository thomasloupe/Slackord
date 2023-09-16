using MenuApp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
                int fileCount = 0;

                var result = await FolderPicker.Default.PickAsync(cancellationToken);
                if (!result.IsSuccessful)
                {
                    return;
                }

                selectedFolder = result.Folder.Path;

                var allFiles = Directory.EnumerateFiles(selectedFolder, "*.json", SearchOption.AllDirectories);
                fileCount = allFiles.Count();

                // Group files by their directory (which represents the channel)
                var groupedFiles = allFiles.GroupBy(file => Path.GetFileName(Path.GetDirectoryName(file)));

                foreach (var group in groupedFiles)
                {
                    var folderName = group.Key;
                    var parser = new Parser();
                    await parser.ParseJsonFiles(group.ToList(), folderName, Channels);
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await MainPage.UpdateParsingMessageProgress(group.Count(), fileCount);
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
