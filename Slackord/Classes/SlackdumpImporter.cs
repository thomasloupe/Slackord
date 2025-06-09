using Newtonsoft.Json.Linq;

namespace Slackord.Classes
{
    /// <summary>
    /// Provides helper methods for processing Slackdump exports
    /// </summary>
    internal static class SlackdumpImporter
    {
        /// <summary>
        /// Processes Slackdump export data from the selected folder
        /// </summary>
        /// <param name="isFullExport">Whether this is a full export or single channel export</param>
        /// <param name="folderPath">The path to the Slackdump export folder</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        internal static async Task ProcessSlackdumpDataAsync(bool isFullExport, string folderPath, CancellationToken cancellationToken)
        {
            DirectoryInfo directoryInfo = new(folderPath);
            DirectoryInfo baseRoot = isFullExport ? directoryInfo : directoryInfo.Parent;

            DirectoryInfo rootDirectory = baseRoot;
            DirectoryInfo channelsDir = baseRoot.GetDirectories("channels").FirstOrDefault();
            if (channelsDir != null)
            {
                rootDirectory = channelsDir;
            }

            FileInfo usersFile = baseRoot.GetFiles("users.json").FirstOrDefault() ?? baseRoot.Parent?.GetFiles("users.json").FirstOrDefault();
            FileInfo channelsFile = baseRoot.GetFiles("channels.json").FirstOrDefault() ?? baseRoot.Parent?.GetFiles("channels.json").FirstOrDefault();

            Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Parsing Users for import...\n"); });

            Dictionary<string, DeconstructedUser> usersDict = usersFile != null ? DeconstructedUsers.ParseUsersFile(usersFile) : [];
            Dictionary<string, string> channelDescriptions = [];

            if (channelsFile != null)
            {
                string channelsJsonContent = await File.ReadAllTextAsync(channelsFile.FullName, cancellationToken).ConfigureAwait(false);
                JArray channelsJson = JArray.Parse(channelsJsonContent);
                channelDescriptions = channelsJson.ToDictionary(
                    jChannel => jChannel["name"].ToString(),
                    jChannel => jChannel["purpose"]["value"].ToString()
                );
            }

            Reconstruct.InitializeUsersDict(usersDict);

            DirectoryInfo[] channelDirectories = isFullExport ? rootDirectory.GetDirectories() : [directoryInfo];
            FileInfo[] rootJsonFiles = rootDirectory.GetFiles("*.json")
                .Where(f => f.Name != "users.json" && f.Name != "channels.json")
                .ToArray();

            bool useRootFiles = channelDirectories.Length == 0 && rootJsonFiles.Length > 0;

            int totalFiles = useRootFiles ? rootJsonFiles.Length : ImportJson.CountTotalJsonFiles(channelDirectories);
            int filesProcessed = 0;

            ApplicationWindow.ShowProgressBar();
            ProcessingManager.Instance.SetState(ProcessingState.DeconstructingMessages);

            if (useRootFiles)
            {
                foreach (FileInfo jsonFile in rootJsonFiles)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        DirectoryInfo fakeDirectory = new(jsonFile.FullName);
                        int channelFilesProcessed = await ImportJson.ProcessChannelAsync(fakeDirectory, channelDescriptions, usersDict, filesProcessed, totalFiles, cancellationToken);
                        filesProcessed += channelFilesProcessed;
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Dispatch(() =>
                        {
                            ApplicationWindow.WriteToDebugWindow($"Exception processing file {jsonFile.Name}: {ex.Message}\n");
                        });
                    }
                }
            }
            else
            {
                foreach (DirectoryInfo channelDirectory in channelDirectories)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        int channelFilesProcessed = await ImportJson.ProcessChannelAsync(channelDirectory, channelDescriptions, usersDict, filesProcessed, totalFiles, cancellationToken);
                        filesProcessed += channelFilesProcessed;
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Exception processing channel {channelDirectory.Name}: {ex.Message}\n"); });
                    }
                }
            }
        }
    }
}
