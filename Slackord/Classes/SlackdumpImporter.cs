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
            Dictionary<string, HashSet<string>> channelPins = [];
            HashSet<string> validChannelNames = [];

            if (channelsFile != null && channelsFile.Exists)
            {
                string channelsJsonContent = await File.ReadAllTextAsync(channelsFile.FullName, cancellationToken).ConfigureAwait(false);
                JArray channelsJson = JArray.Parse(channelsJsonContent);

                foreach (JObject channel in channelsJson.Cast<JObject>())
                {
                    string channelId = channel["id"]?.ToString();
                    string channelName = channel["name"]?.ToString();
                    bool isChannel = channel["is_channel"]?.Value<bool>() ?? false;

                    if (!string.IsNullOrEmpty(channelName) && isChannel && !channelId.StartsWith('D'))
                    {
                        validChannelNames.Add(channelName);
                        channelDescriptions[channelName] = channel["purpose"]?["value"]?.ToString() ?? "";

                        HashSet<string> pins = [];
                        if (channel["pins"] is JArray pinsArray)
                        {
                            foreach (JObject pin in pinsArray.Cast<JObject>())
                            {
                                string pinId = pin["id"]?.ToString();
                                if (!string.IsNullOrEmpty(pinId))
                                {
                                    pins.Add(pinId);
                                }
                            }
                        }
                        channelPins[channelName] = pins;
                    }
                }
            }

            Reconstruct.InitializeUsersDict(usersDict);

            DirectoryInfo[] allDirectories = isFullExport ? rootDirectory.GetDirectories() : [directoryInfo];
            DirectoryInfo[] channelDirectories;

            if (validChannelNames.Count > 0)
            {
                channelDirectories = [.. allDirectories.Where(d => validChannelNames.Contains(d.Name))];
            }
            else
            {
                channelDirectories = [.. allDirectories.Where(d =>
            !d.Name.StartsWith('D') &&
            !d.Name.StartsWith('G') &&
            !d.Name.StartsWith("__"))];
            }

            if (channelDirectories.Length == 0)
            {
                Application.Current.Dispatcher.Dispatch(() => {
                    ApplicationWindow.WriteToDebugWindow($"❌ No valid channel directories found to process!\n");
                });
                return;
            }

            FileInfo[] rootJsonFiles = [.. rootDirectory.GetFiles("*.json").Where(f =>
        f.Name != "users.json" &&
        f.Name != "channels.json" &&
        f.Name != "meta.json" &&
        f.Name != "mpims.json" &&
        f.Name != "dms.json")];

            bool useRootFiles = channelDirectories.Length == 0 && rootJsonFiles.Length > 0;
            int totalFiles = useRootFiles ? rootJsonFiles.Length : ImportJson.CountTotalJsonFiles(channelDirectories);
            int filesProcessed = 0;

            ApplicationWindow.ShowProgressBar();
            ProcessingManager.Instance.SetState(ProcessingState.DeconstructingMessages);

            foreach (DirectoryInfo channelDirectory in channelDirectories)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int channelFilesProcessed = await ImportJson.ProcessChannelAsync(channelDirectory, channelDescriptions, channelPins, usersDict, filesProcessed, totalFiles, cancellationToken);
                    filesProcessed += channelFilesProcessed;
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Dispatch(() => {
                        ApplicationWindow.WriteToDebugWindow($"Exception processing channel {channelDirectory.Name}: {ex.Message}\n");
                    });
                }
            }
        }
    }
}
