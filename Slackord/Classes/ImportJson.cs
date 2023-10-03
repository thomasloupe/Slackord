using Newtonsoft.Json.Linq;
using CommunityToolkit.Maui.Storage;
using MenuApp;

namespace Slackord.Classes
{
    public class ImportJson
    {
        public static string RootFolderPath { get; private set; }
        public static List<Channel> Channels { get; set; } = new List<Channel>();

        public static async Task ImportJsonAsync(CancellationToken cancellationToken)
        {
            ApplicationWindow.HideProgressBar();
            try
            {
                var picker = await FolderPicker.Default.PickAsync(cancellationToken);

                // Check if picker is null or if its Folder property is null.
                if (picker == null || picker.Folder == null)
                {
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Exception(1000) in FolderPicker : Import canceled by user.\nDue to platform requirements, Slackord must be restarted when you cancel the folder browser.\nCurrently, there's no workaround. Sorry about this!\n"); });
                    return;
                }

                var folderPath = picker.Folder.Path;
                RootFolderPath = folderPath;
                Dictionary<string, DeconstructedUser> usersDict = null;
                if (!string.IsNullOrEmpty(folderPath))
                {
                    var result = await ConvertAsync(folderPath, cancellationToken);
                    Channels = result.Channels;
                    usersDict = result.UsersDict;
                }

                // Populate UsersDict in Reconstruct before calling ReconstructAsync.
                Reconstruct.InitializeUsersDict(usersDict);

                // This checks whether any folder was selected and whether any channels were deconstructed.
                if (!string.IsNullOrEmpty(RootFolderPath) && Channels.Count != 0)
                {
                    // Call ReconstructAsync to reconstruct messages for Discord
                    await Reconstruct.ReconstructAsync(Channels, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ImportJsonAsync() : {ex.Message}\n"); });
                return;
            }
        }

        public static async Task<(List<Channel> Channels, Dictionary<string, DeconstructedUser> UsersDict)> ConvertAsync(string folderPath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var channels = new List<Channel>();
            var directoryInfo = new DirectoryInfo(folderPath);
            var channelDirectories = directoryInfo.GetDirectories();
            var usersFile = directoryInfo.GetFiles("users.json").FirstOrDefault();
            var channelsFile = directoryInfo.GetFiles("channels.json").FirstOrDefault();

            Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Parsing Users for import...\n"); });

            var usersDict = DeconstructedUsers.ParseUsersFile(usersFile);

            // Parse channels.json to get channel descriptions.
            Dictionary<string, string> channelDescriptions = null;
            if (channelsFile != null)
            {
                var channelsJsonContent = await File.ReadAllTextAsync(channelsFile.FullName, cancellationToken).ConfigureAwait(false);
                var channelsJson = JArray.Parse(channelsJsonContent);
                channelDescriptions = channelsJson.ToDictionary(
                    jChannel => jChannel["name"].ToString(),
                    jChannel => jChannel["purpose"]["value"].ToString()
                );
            }

            int totalFiles = CountTotalJsonFiles(channelDirectories);
            int filesProcessed = 0;

            foreach (var channelDirectory in channelDirectories)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var channel = new Channel { Name = channelDirectory.Name };
                    var jsonFiles = channelDirectory.GetFiles("*.json");
                    int jsonFileCount = jsonFiles.Length;
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Begin parsing JSON data for {channel.Name} with {jsonFileCount} JSON files...\n"); });

                    if (jsonFileCount > 400)
                    {
                        Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"This import appears to be quite large. Reconstructing will take a very long time and the UI may freeze until completed. Please be patient!\nDeconstruction/Reconstruction process started..."); });
                    }

                    foreach (var jsonFile in jsonFiles)
                    {
                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var jsonContent = await File.ReadAllTextAsync(jsonFile.FullName, cancellationToken).ConfigureAwait(false);
                            var messagesArray = JArray.Parse(jsonContent);

                            foreach (JObject slackMessage in messagesArray.Cast<JObject>())
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var deconstructedMessage = Deconstruct.DeconstructMessage(slackMessage);
                                channel.DeconstructedMessagesList.Add(deconstructedMessage);
                            }

                            filesProcessed++;
                            ApplicationWindow.UpdateProgressBar(filesProcessed, totalFiles, "files");
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Exception processing file {jsonFile.Name}: {ex.Message}\n"); });
                        }
                    }

                    // Look up the description for the channel and set it on the Channel object
                    if (channelDescriptions != null && channelDescriptions.TryGetValue(channel.Name, out var description))
                    {
                        channel.Description = description;
                    }
                    channels.Add(channel);
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Completed importing channel {channel.Name}.\n\n"); });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Exception processing channel {channelDirectory.Name}: {ex.Message}\n"); });
                }
            }

            return (channels, usersDict);
        }

        private static int CountTotalJsonFiles(DirectoryInfo[] channelDirectories)
        {
            int totalFiles = 0;
            foreach (var channelDirectory in channelDirectories)
            {
                totalFiles += channelDirectory.GetFiles("*.json").Length;
            }

            ApplicationWindow.ResetProgressBar();
            ApplicationWindow.ShowProgressBar();
            return totalFiles;
        }
    }
}
