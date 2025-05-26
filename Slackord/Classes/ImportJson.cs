using CommunityToolkit.Maui.Storage;
using Newtonsoft.Json.Linq;

namespace Slackord.Classes
{
    public class ImportJson
    {
        public static string RootFolderPath { get; private set; }
        public static ImportSession CurrentSession { get; private set; }
        public static int TotalHiddenFileCount { get; internal set; } = 0;

        public static async Task ImportJsonAsync(bool isFullExport, CancellationToken cancellationToken)
        {
            ProcessingManager.Instance.SetState(ProcessingState.ImportingFiles);
            ApplicationWindow.HideProgressBar();

            // Reset counts
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

                // Create new import session
                CurrentSession = ImportSession.CreateNew();
                ApplicationWindow.WriteToDebugWindow($"📁 Created new import session: {CurrentSession.SessionId}\n");
                ApplicationWindow.WriteToDebugWindow($"💾 Session data will be saved to: {CurrentSession.SessionPath}\n\n");

                if (!string.IsNullOrEmpty(folderPath))
                {
                    await ProcessSlackDataAsync(isFullExport, folderPath, cancellationToken);
                }

                if (TotalHiddenFileCount > 0)
                {
                    Application.Current.Dispatcher.Dispatch(() =>
                    {
                        ApplicationWindow.WriteToDebugWindow($"ℹ️ Note: {TotalHiddenFileCount:N0} files were hidden by Slack due to limits\n");
                    });
                }

                // Display completion summary
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

        private static async Task ProcessSlackDataAsync(bool isFullExport, string folderPath, CancellationToken cancellationToken)
        {
            DirectoryInfo directoryInfo = new(folderPath);
            DirectoryInfo rootDirectory = isFullExport ? directoryInfo : directoryInfo.Parent;

            // Load users and channel descriptions
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

            // Initialize UsersDict in Reconstruct
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
            // Deconstruct all messages first
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
            // Now reconstruct messages and save to .slackord file
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

        private static async Task ReconstructAndSaveChannelAsync(string channelName, List<DeconstructedMessage> deconstructedMessages,
            ChannelProgress channelProgress, CancellationToken cancellationToken)
        {
            try
            {
                Application.Current.Dispatcher.Dispatch(() => {
                    ApplicationWindow.WriteToDebugWindow($"🔨 Reconstructing {deconstructedMessages.Count:N0} messages for {channelName}...\n");
                });

                var reconstructedMessages = new List<ReconstructedMessage>();
                int processedCount = 0;

                foreach (var deconstructedMessage in deconstructedMessages)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Create a temporary channel object for the reconstruction process
                    var tempChannel = new Channel { Name = channelName };
                    await Reconstruct.ReconstructMessage(deconstructedMessage, tempChannel);

                    // Add the reconstructed messages to our list
                    reconstructedMessages.AddRange(tempChannel.ReconstructedMessagesList);

                    processedCount++;

                    // Update progress every 100 messages to avoid UI spam
                    if (processedCount % 100 == 0 || processedCount == deconstructedMessages.Count)
                    {
                        Application.Current.Dispatcher.Dispatch(() => {
                            ApplicationWindow.WriteToDebugWindow($"  📋 Processed {processedCount:N0}/{deconstructedMessages.Count:N0} messages for {channelName}\n");
                        });
                    }
                }

                // Update the channel progress with actual reconstructed count
                channelProgress.TotalMessages = reconstructedMessages.Count;

                // Save to .slackord file
                string channelFilePath = CurrentSession.GetChannelFilePath(channelName);
                await SlackordFileManager.SaveChannelMessagesAsync(channelFilePath, reconstructedMessages);

                channelProgress.FileCreated = true;
                CurrentSession.Save();

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

            // Count threads per channel for the thread warning
            DisplayThreadWarning();
        }

        private static void DisplayThreadWarning()
        {
            // This would need to be updated to work with the file-based system
            // For now, we'll skip the thread count since we don't keep everything in memory
            Application.Current.Dispatcher.Dispatch(() => {
                ApplicationWindow.WriteToDebugWindow($"ℹ️ Thread counts will be calculated during Discord import.\n");
                ApplicationWindow.WriteToDebugWindow($"💡 If you have many threads, you may need to manage Discord's 1000 thread limit.\n\n");
            });
        }

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
        public static ImportSession GetCurrentSession()
        {
            return CurrentSession;
        }

        /// <summary>
        /// Sets the current session (used for resume operations)
        /// </summary>
        public static void SetCurrentSession(ImportSession session)
        {
            CurrentSession = session;
        }
    }
}