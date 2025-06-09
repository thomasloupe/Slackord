using CommunityToolkit.Maui.Storage;
using Newtonsoft.Json.Linq;

namespace Slackord.Classes
{
    /// <summary>
    /// Handles the import and processing of Slack JSON export data
    /// </summary>
    public class ImportJson
    {
        /// <summary>
        /// Gets the root folder path of the selected Slack export
        /// </summary>
        public static string RootFolderPath { get; private set; }

        /// <summary>
        /// Gets the current import session being processed
        /// </summary>
        public static ImportSession CurrentSession { get; private set; }

        /// <summary>
        /// Gets or sets the total count of files hidden by Slack due to limits
        /// </summary>
        public static int TotalHiddenFileCount { get; internal set; } = 0;

        /// <summary>
        /// Initiates the JSON import process from a selected folder
        /// </summary>
        /// <param name="isFullExport">Whether this is a full Slack export or single channel export</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        public static async Task ImportJsonAsync(bool isFullExport, CancellationToken cancellationToken)
        {
            ProcessingManager.Instance.SetState(ProcessingState.ImportingFiles);
            ApplicationWindow.HideProgressBar();

            TotalHiddenFileCount = 0;
            ApplicationWindow.ResetProgressBar();

            try
            {
                FolderPickerResult picker = await FolderPicker.Default.PickAsync(cancellationToken);

                if (picker == null || picker.Folder == null)
                {
                    ProcessingManager.Instance.SetState(ProcessingState.Idle);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow("Folder selection was cancelled.\n"); });
                        return;
                    }
                    else
                    {
                        return;
                    }
                }

                string folderPath = picker.Folder.Path;
                RootFolderPath = folderPath;

                CurrentSession = ImportSession.CreateNew();
                ApplicationWindow.WriteToDebugWindow($"📁 Created new import session: {CurrentSession.SessionId}\n");
                ApplicationWindow.WriteToDebugWindow($"💾 Session data will be saved to: {CurrentSession.SessionPath}\n\n");

                if (!string.IsNullOrEmpty(folderPath))
                {
                    bool isSlackdump = File.Exists(Path.Combine(folderPath, "meta.json")) ||
                                       File.Exists(Path.Combine(folderPath, "files", "slackdump.json"));

                    if (isSlackdump)
                    {
                        await ProcessSlackdumpDataAsync(folderPath, cancellationToken);
                    }
                    else
                    {
                        await ProcessSlackDataAsync(isFullExport, folderPath, cancellationToken);
                    }
                }

                if (TotalHiddenFileCount > 0)
                {
                    Application.Current.Dispatcher.Dispatch(() =>
                    {
                        ApplicationWindow.WriteToDebugWindow($"ℹ️ Note: {TotalHiddenFileCount:N0} files were hidden by Slack due to limits\n");
                    });
                }

                DisplayImportSummary();
                ProcessingManager.Instance.SetState(ProcessingState.ReadyForDiscordImport);
            }
            catch (OperationCanceledException)
            {
                ProcessingManager.Instance.SetState(ProcessingState.Error);
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow("Import operation was cancelled.\n"); });
            }
            catch (Exception ex)
            {
                ProcessingManager.Instance.SetState(ProcessingState.Error);
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ImportJsonAsync() : {ex.Message}\n"); });
                return;
            }
        }

        /// <summary>
        /// Processes the Slack export data from the selected folder
        /// </summary>
        /// <param name="isFullExport">Whether this is a full export or single channel export</param>
        /// <param name="folderPath">The path to the Slack export folder</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        private static async Task ProcessSlackDataAsync(bool isFullExport, string folderPath, CancellationToken cancellationToken)
        {
            DirectoryInfo directoryInfo = new(folderPath);
            DirectoryInfo rootDirectory = isFullExport ? directoryInfo : directoryInfo.Parent;

            FileInfo usersFile = rootDirectory.GetFiles("users.json").FirstOrDefault();
            FileInfo channelsFile = rootDirectory.GetFiles("channels.json").FirstOrDefault();

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
            int totalFiles = CountTotalJsonFiles(channelDirectories);
            int filesProcessed = 0;

            ApplicationWindow.ShowProgressBar();
            ProcessingManager.Instance.SetState(ProcessingState.DeconstructingMessages);

            foreach (DirectoryInfo channelDirectory in channelDirectories)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int channelFilesProcessed = await ProcessChannelAsync(channelDirectory, channelDescriptions, usersDict, filesProcessed, totalFiles, cancellationToken);
                    filesProcessed += channelFilesProcessed;
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Exception processing channel {channelDirectory.Name}: {ex.Message}\n"); });
                }
            }
        }

        /// <summary>
        /// Processes Slackdump export data
        /// </summary>
        /// <param name="folderPath">Path to the Slackdump root folder</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        private static async Task ProcessSlackdumpDataAsync(string folderPath, CancellationToken cancellationToken)
        {
            DirectoryInfo rootDirectory = new(folderPath);

            FileInfo metaFile = rootDirectory.GetFiles("meta.json").FirstOrDefault();
            JObject metaJson = new();
            if (metaFile != null)
            {
                string metaContent = await File.ReadAllTextAsync(metaFile.FullName, cancellationToken).ConfigureAwait(false);
                metaJson = JObject.Parse(metaContent);
            }

            Dictionary<string, DeconstructedUser> usersDict = [];
            if (metaJson["users"] is JArray usersArray)
            {
                foreach (JObject userObject in usersArray.Cast<JObject>())
                {
                    DeconstructedUser deconstructedUser = userObject.ToObject<DeconstructedUser>();
                    if (deconstructedUser != null && deconstructedUser.Id != null)
                    {
                        usersDict[deconstructedUser.Id] = deconstructedUser;
                    }
                }
            }

            Dictionary<string, string> channelIdToName = [];
            Dictionary<string, string> channelDescriptions = [];
            foreach (string section in new[] { "channels", "groups", "ims", "mpims" })
            {
                if (metaJson[section] is JArray channelsArray)
                {
                    foreach (JObject channelObject in channelsArray.Cast<JObject>())
                    {
                        string id = channelObject["id"]?.ToString();
                        string name = channelObject["name"]?.ToString();
                        string purpose = channelObject["purpose"]?["value"]?.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                        {
                            channelIdToName[id] = name;
                            channelDescriptions[name] = purpose;
                        }
                    }
                }
            }

            Reconstruct.InitializeUsersDict(usersDict);

            var channelDirectories = new List<DirectoryInfo>();
            foreach (string dirName in new[] { "channels", "groups", "ims", "mpims" })
            {
                DirectoryInfo container = rootDirectory.GetDirectories(dirName).FirstOrDefault();
                if (container != null)
                {
                    channelDirectories.AddRange(container.GetDirectories());
                }
            }

            if (channelDirectories.Count == 0)
            {
                channelDirectories.AddRange(rootDirectory.GetDirectories());
            }

            int totalFiles = CountTotalJsonFiles(channelDirectories.ToArray());
            int filesProcessed = 0;

            ApplicationWindow.ShowProgressBar();
            ProcessingManager.Instance.SetState(ProcessingState.DeconstructingMessages);

            foreach (DirectoryInfo channelDirectory in channelDirectories)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string channelId = channelDirectory.Name;
                    string channelName = channelIdToName.TryGetValue(channelId, out string name) ? name : channelId;
                    int channelFilesProcessed = await ProcessSlackdumpChannelAsync(channelDirectory, channelName, channelDescriptions, filesProcessed, totalFiles, cancellationToken);
                    filesProcessed += channelFilesProcessed;
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Exception processing channel {channelDirectory.Name}: {ex.Message}\n"); });
                }
            }
        }

        /// <summary>
        /// Processes all JSON files within a Slackdump channel directory
        /// </summary>
        private static async Task<int> ProcessSlackdumpChannelAsync(DirectoryInfo channelDirectory, string channelName,
                    Dictionary<string, string> channelDescriptions, int currentFilesProcessed, int totalFiles, CancellationToken cancellationToken)
        {
            FileInfo[] jsonFiles = channelDirectory.GetFiles("*.json");
            int jsonFileCount = jsonFiles.Length;
            int localFilesProcessed = 0;

            Application.Current.Dispatcher.Dispatch(() =>
            {
                ApplicationWindow.WriteToDebugWindow($"Begin processing {channelName} with {jsonFileCount} JSON files...\n");
            });
            if (jsonFileCount > 400)
            {
                Application.Current.Dispatcher.Dispatch(() =>
                {
                    ApplicationWindow.WriteToDebugWindow($"Large import detected for {channelName}. This may take some time...\n");
                });
            }

            var deconstructedMessages = new List<DeconstructedMessage>();
            foreach (FileInfo jsonFile in jsonFiles)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string jsonContent = await File.ReadAllTextAsync(jsonFile.FullName, cancellationToken).ConfigureAwait(false);
                    JArray messagesArray = JArray.Parse(jsonContent);
                    foreach (JObject slackMessage in messagesArray.Cast<JObject>())
                    {
                        if (slackMessage["files"] is JArray filesArray)
                        {
                            foreach (JObject fileObj in filesArray.Cast<JObject>())
                            {
                                if (fileObj["url_private_download"] == null && fileObj["id"] != null)
                                {
                                    string fileId = fileObj["id"].ToString();
                                    string fileName = fileObj["name"]?.ToString() ?? fileId;
                                    string localPath = Path.Combine(RootFolderPath, "files", fileId, fileName);
                                    fileObj["url_private_download"] = localPath;
                                }
                            }
                        }

                        DeconstructedMessage deconstructedMessage = Deconstruct.DeconstructMessage(slackMessage);
                        deconstructedMessages.Add(deconstructedMessage);
                    }
                    localFilesProcessed++;
                    ApplicationWindow.UpdateProgressBar(currentFilesProcessed + localFilesProcessed, totalFiles, "files");
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Dispatch(() =>
                    {
                        ApplicationWindow.WriteToDebugWindow($"Exception processing file {jsonFile.Name}: {ex.Message}\n");
                    });
                }
            }

            if (deconstructedMessages.Count > 0)
            {
                ProcessingManager.Instance.SetState(ProcessingState.ReconstructingMessages);
                var channelProgress = CurrentSession.AddChannel(channelName, deconstructedMessages.Count);
                if (channelDescriptions.TryGetValue(channelName, out string description))
                {
                    channelProgress.Description = description;
                }
                await ReconstructAndSaveChannelAsync(channelName, deconstructedMessages, channelProgress, cancellationToken);
            }

            return localFilesProcessed;
        }

        /// <summary>
        /// Processes all JSON files within a single channel directory
        /// </summary>
        /// <param name="channelDirectory">The channel directory to process</param>
        /// <param name="channelDescriptions">Dictionary of channel descriptions</param>
        /// <param name="usersDict">Dictionary of user information</param>
        /// <param name="currentFilesProcessed">Number of files already processed</param>
        /// <param name="totalFiles">Total number of files to process</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The number of files processed in this channel</returns>
        private static async Task<int> ProcessChannelAsync(DirectoryInfo channelDirectory, Dictionary<string, string> channelDescriptions,
                    Dictionary<string, DeconstructedUser> usersDict, int currentFilesProcessed, int totalFiles, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(usersDict);
            string channelName = channelDirectory.Name;
            FileInfo[] jsonFiles = channelDirectory.GetFiles("*.json");
            int jsonFileCount = jsonFiles.Length;
            int localFilesProcessed = 0;

            Application.Current.Dispatcher.Dispatch(() => {
                ApplicationWindow.WriteToDebugWindow($"Begin processing {channelName} with {jsonFileCount} JSON files...\n");
            });
            if (jsonFileCount > 400)
            {
                Application.Current.Dispatcher.Dispatch(() => {
                    ApplicationWindow.WriteToDebugWindow($"Large import detected for {channelName}. This may take some time...\n");
                });
            }

            var deconstructedMessages = new List<DeconstructedMessage>();
            foreach (FileInfo jsonFile in jsonFiles)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string jsonContent = await File.ReadAllTextAsync(jsonFile.FullName, cancellationToken).ConfigureAwait(false);
                    JArray messagesArray = JArray.Parse(jsonContent);
                    foreach (JObject slackMessage in messagesArray.Cast<JObject>())
                    {
                        DeconstructedMessage deconstructedMessage = Deconstruct.DeconstructMessage(slackMessage);
                        deconstructedMessages.Add(deconstructedMessage);
                    }
                    localFilesProcessed++;
                    ApplicationWindow.UpdateProgressBar(currentFilesProcessed + localFilesProcessed, totalFiles, "files");
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Dispatch(() => {
                        ApplicationWindow.WriteToDebugWindow($"Exception processing file {jsonFile.Name}: {ex.Message}\n");
                    });
                }
            }

            if (deconstructedMessages.Count > 0)
            {
                ProcessingManager.Instance.SetState(ProcessingState.ReconstructingMessages);
                var channelProgress = CurrentSession.AddChannel(channelName, deconstructedMessages.Count);
                if (channelDescriptions.TryGetValue(channelName, out string description))
                {
                    channelProgress.Description = description;
                }
                await ReconstructAndSaveChannelAsync(channelName, deconstructedMessages, channelProgress, cancellationToken);
            }

            return localFilesProcessed;
        }

        /// <summary>
        /// Reconstructs messages for a channel and saves them to a .slackord file
        /// </summary>
        /// <param name="channelName">The name of the channel being processed</param>
        /// <param name="deconstructedMessages">The list of deconstructed messages</param>
        /// <param name="channelProgress">The progress tracker for this channel</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        private static async Task ReconstructAndSaveChannelAsync(string channelName, List<DeconstructedMessage> deconstructedMessages,
            ChannelProgress channelProgress, CancellationToken cancellationToken)
        {
            try
            {
                Application.Current.Dispatcher.Dispatch(() => {
                    ApplicationWindow.WriteToDebugWindow($"🔨 Reconstructing for Discord...\n");
                });

                var reconstructedMessages = new List<ReconstructedMessage>();
                int processedCount = 0;
                var loggedPercents = new HashSet<int>();
                int totalMessages = deconstructedMessages.Count;

                if (totalMessages > 100)
                {
                    Application.Current.Dispatcher.Dispatch(() => {
                        ApplicationWindow.WriteToDebugWindow($"🔨 Reconstructing {totalMessages:N0} messages for {channelName}...\n");
                    });
                }

                const int batchSize = 50;
                for (int i = 0; i < deconstructedMessages.Count; i += batchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batch = deconstructedMessages.Skip(i).Take(batchSize).ToList();

                    await Task.Run(async () =>
                    {
                        foreach (var deconstructedMessage in batch)
                        {
                            var tempChannel = new Channel { Name = channelName };
                            await Reconstruct.ReconstructMessage(deconstructedMessage, tempChannel);
                            reconstructedMessages.AddRange(tempChannel.ReconstructedMessagesList);
                        }
                    }, cancellationToken);

                    processedCount += batch.Count;

                    if (totalMessages >= 100)
                    {
                        double progress = (double)processedCount / totalMessages;
                        int percent = (int)(progress * 100);
                        int roundedPercent = (percent / 10) * 10;

                        if (!loggedPercents.Contains(roundedPercent) && processedCount < totalMessages)
                        {
                            loggedPercents.Add(roundedPercent);
                            Application.Current.Dispatcher.Dispatch(() => {
                                ApplicationWindow.WriteToDebugWindow(
                                    $"  📋 Processed {processedCount:N0}/{totalMessages:N0} messages for {channelName} ({roundedPercent}%)\n"
                                );
                            });
                        }
                    }

                    await Task.Yield();
                }

                if (totalMessages >= 100 && !loggedPercents.Contains(100))
                {
                    Application.Current.Dispatcher.Dispatch(() => {
                        ApplicationWindow.WriteToDebugWindow(
                            $"  📋 Processed {totalMessages:N0}/{totalMessages:N0} messages for {channelName} (100%)\n"
                        );
                    });
                }

                channelProgress.TotalMessages = reconstructedMessages.Count;

                // Save to file
                string channelFilePath = CurrentSession.GetChannelFilePath(channelName);
                await Task.Run(async () =>
                {
                    await SlackordFileManager.SaveChannelMessagesAsync(channelFilePath, reconstructedMessages);
                }, cancellationToken);

                channelProgress.FileCreated = true;
                CurrentSession.Save();

                // Show saved message count
                Application.Current.Dispatcher.Dispatch(() => {
                    ApplicationWindow.WriteToDebugWindow($"💾 Saved {reconstructedMessages.Count:N0} messages to {channelName}.slackord\n");
                });

                Application.Current.Dispatcher.Dispatch(() => {
                    ApplicationWindow.WriteToDebugWindow($"✅ Completed {channelName}: {reconstructedMessages.Count:N0} messages saved\n\n");
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Dispatch(() => {
                    ApplicationWindow.WriteToDebugWindow($"❌ Error reconstructing {channelName}: {ex.Message}\n");
                });
                Logger.Log($"ReconstructAndSaveChannelAsync error for {channelName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Displays a summary of the completed import operation
        /// </summary>
        private static void DisplayImportSummary()
        {
            if (CurrentSession == null || CurrentSession.Channels.Count == 0)
                return;

            int totalChannels = CurrentSession.Channels.Count;
            int totalMessages = CurrentSession.Channels.Sum(c => c.TotalMessages);

            Application.Current.Dispatcher.Dispatch(() => {
                ApplicationWindow.WriteToDebugWindow($"\n🎊 IMPORT COMPLETE! 🎊\n");
                ApplicationWindow.WriteToDebugWindow($"📊 Summary:\n");
                ApplicationWindow.WriteToDebugWindow($"   • Session: {CurrentSession.SessionId}\n");
                ApplicationWindow.WriteToDebugWindow($"   • Channels processed: {totalChannels:N0}\n");
                ApplicationWindow.WriteToDebugWindow($"   • Total messages ready: {totalMessages:N0}\n");
                ApplicationWindow.WriteToDebugWindow($"   • Data saved to: {CurrentSession.SessionPath}\n\n");

                foreach (var channel in CurrentSession.Channels)
                {
                    ApplicationWindow.WriteToDebugWindow($"   📁 {channel.Name}: {channel.TotalMessages:N0} messages\n");
                }

                ApplicationWindow.WriteToDebugWindow($"\n🚀 Ready for Discord import! Use the '/slackord' command to begin.\n\n");
            });

            DisplayThreadWarning();
        }

        /// <summary>
        /// Displays information about thread handling during import
        /// </summary>
        private static void DisplayThreadWarning()
        {
            Application.Current.Dispatcher.Dispatch(() => {
                ApplicationWindow.WriteToDebugWindow($"ℹ️ Thread counts will be calculated during Discord import.\n");
                ApplicationWindow.WriteToDebugWindow($"💡 If you have many threads, you may need to manage Discord's 1000 thread limit.\n\n");
            });
        }

        /// <summary>
        /// Counts the total number of JSON files across all channel directories
        /// </summary>
        /// <param name="channelDirectories">Array of channel directories to count files in</param>
        /// <returns>The total number of JSON files</returns>
        private static int CountTotalJsonFiles(DirectoryInfo[] channelDirectories)
        {
            int totalFiles = 0;
            foreach (DirectoryInfo channelDirectory in channelDirectories)
            {
                totalFiles += channelDirectory.GetFiles("*.json").Length;
            }

            ApplicationWindow.ResetProgressBar();
            ApplicationWindow.ShowProgressBar();
            return totalFiles;
        }

        /// <summary>
        /// Gets the current session or loads an existing one
        /// </summary>
        /// <returns>The current import session</returns>
        public static ImportSession GetCurrentSession()
        {
            return CurrentSession;
        }

        /// <summary>
        /// Sets the current session (used for resume operations)
        /// </summary>
        /// <param name="session">The import session to set as current</param>
        public static void SetCurrentSession(ImportSession session)
        {
            CurrentSession = session;
        }
    }
}