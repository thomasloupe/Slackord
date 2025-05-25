using CommunityToolkit.Maui.Storage;
using Newtonsoft.Json.Linq;

namespace Slackord.Classes
{
    public class ImportJson
    {
        public static string RootFolderPath { get; private set; }
        public static List<Channel> Channels { get; set; } = [];
        public static int TotalHiddenFileCount { get; internal set; } = 0;

        public static async Task ImportJsonAsync(bool isFullExport, CancellationToken cancellationToken)
        {
            ProcessingManager.Instance.SetState(ProcessingState.ImportingFiles);
            ApplicationWindow.HideProgressBar();

            // Clear existing data and reset counts
            Channels.Clear();
            TotalHiddenFileCount = 0;
            ApplicationWindow.ResetProgressBar();

            // Load resume data
            List<ResumeData> resumeDataList = ResumeData.LoadResumeData();

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
                Dictionary<string, DeconstructedUser> usersDict = null;

                if (!string.IsNullOrEmpty(folderPath))
                {
                    (List<Channel> Channels, Dictionary<string, DeconstructedUser> UsersDict) result = await ConvertAsync(isFullExport, folderPath, cancellationToken);
                    Channels = result.Channels;
                    usersDict = result.UsersDict;
                }

                // Initialize UsersDict in Reconstruct
                Reconstruct.InitializeUsersDict(usersDict);

                // Check for channels to resume
                foreach (var channel in Channels)
                {
                    var resumeData = resumeDataList.FirstOrDefault(rd => rd.ChannelName == channel.Name);
                    if (resumeData != null && !resumeData.ImportedToDiscord)
                    {
                        bool shouldResume = await ResumeData.AskUserToResumeChannel(resumeData.ChannelName);
                        if (shouldResume)
                        {
                            await ResumeData.InitializeChannelForResume(channel, resumeData);
                        }
                    }
                }

                // Proceed with reconstruction
                if (!string.IsNullOrEmpty(RootFolderPath) && Channels.Count != 0)
                {
                    // Set state to deconstructing (file processing is done, now processing messages)
                    ProcessingManager.Instance.SetState(ProcessingState.DeconstructingMessages);

                    await Reconstruct.ReconstructAsync(Channels, cancellationToken);

                    if (TotalHiddenFileCount > 0)
                    {
                        Application.Current.Dispatcher.Dispatch(() =>
                        {
                            ApplicationWindow.WriteToDebugWindow($"ℹ️ Note: {TotalHiddenFileCount:N0} files were hidden by Slack due to limits\n");
                        });
                    }

                    // Count threads per channel and display the results
                    Dictionary<string, int> channelThreadCounts = CountThreadsPerChannel();
                    DisplayThreadCounts(channelThreadCounts);
                }
                else
                {
                    ProcessingManager.Instance.SetState(ProcessingState.Idle);
                }
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

        private static async Task<(List<Channel> Channels, Dictionary<string, DeconstructedUser> UsersDict)> ConvertAsync(bool isFullExport, string folderPath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DirectoryInfo directoryInfo = new(folderPath);
            DirectoryInfo rootDirectory = isFullExport ? directoryInfo : directoryInfo.Parent;

            // Fetch users.json and channels.json from the appropriate root directory.
            FileInfo usersFile = rootDirectory.GetFiles("users.json").FirstOrDefault();
            FileInfo channelsFile = rootDirectory.GetFiles("channels.json").FirstOrDefault();

            _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Parsing Users for import...\n"); });

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

            DirectoryInfo[] channelDirectories = isFullExport ? rootDirectory.GetDirectories() : [directoryInfo];

            List<Channel> channels = [];
            int totalFiles = CountTotalJsonFiles(channelDirectories);
            int filesProcessed = 0;

            foreach (DirectoryInfo channelDirectory in channelDirectories)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Channel channel = new() { Name = channelDirectory.Name };
                    FileInfo[] jsonFiles = channelDirectory.GetFiles("*.json");
                    int jsonFileCount = jsonFiles.Length;
                    _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Begin parsing JSON data for {channel.Name} with {jsonFileCount} JSON files...\n"); });

                    if (jsonFileCount > 400)
                    {
                        _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"This import appears to be quite large. Reconstructing will take a very long time and the UI may freeze until completed. Please be patient!\nDeconstruction/Reconstruction process started...\n"); });
                    }

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
                                channel.DeconstructedMessagesList.Add(deconstructedMessage);
                            }

                            filesProcessed++;
                            ApplicationWindow.UpdateProgressBar(filesProcessed, totalFiles, "files");
                        }
                        catch (Exception ex)
                        {
                            _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Exception processing file {jsonFile.Name}: {ex.Message}\n"); });
                        }
                    }

                    if (channelDescriptions != null && channelDescriptions.TryGetValue(channel.Name, out string description))
                    {
                        channel.Description = description;
                    }
                    channels.Add(channel);
                    _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Completed importing channel {channel.Name}.\n\n"); });
                }
                catch (Exception ex)
                {
                    _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Exception processing channel {channelDirectory.Name}: {ex.Message}\n"); });
                }
            }

            return (channels, usersDict);
        }

        private static Dictionary<string, int> CountThreadsPerChannel()
        {
            Dictionary<string, int> channelThreadCounts = [];

            foreach (var channel in Channels)
            {
                int threadCount = channel.DeconstructedMessagesList
                    .Where(m => !string.IsNullOrEmpty(m.ThreadTs))
                    .GroupBy(m => m.ThreadTs)
                    .Count();
                channelThreadCounts[channel.Name] = threadCount;
            }

            return channelThreadCounts;
        }

        private static void DisplayThreadCounts(Dictionary<string, int> channelThreadCounts)
        {
            int totalThreads = channelThreadCounts.Values.Sum();

            foreach (var channel in channelThreadCounts)
            {
                ApplicationWindow.WriteToDebugWindow($"Found {channel.Value} threads in {channel.Key}.\n");
            }

            if (totalThreads > 1000)
            {
                ApplicationWindow.WriteToDebugWindow("WARNING: Guild thread limit exceeds 1000. You will need to close threads on your own, manually to prevent threads from failing to create! The number of threads per channel are listed below." +
                    "\n Please visit https://github.com/thomasloupe/Slackord/blob/main/docs/FAQ.md on how to handle this limitation.\n");
                foreach (var channel in channelThreadCounts)
                {
                    ApplicationWindow.WriteToDebugWindow($"{channel.Key} ({channel.Value})\n");
                }
            }
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
    }
}
