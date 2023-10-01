using Newtonsoft.Json.Linq;
using CommunityToolkit.Maui.Storage;
using MenuApp;
using static Slackord.Classes.DeconstructedUser;

namespace Slackord.Classes
{
    public class ImportJson
    {
        public static string RootFolderPath { get; private set; }
        public static List<Channel> Channels { get; set; } = new List<Channel>();

        public static async Task ImportJsonAsync(CancellationToken cancellationToken)
        {
            try
            {
                var picker = await FolderPicker.Default.PickAsync(cancellationToken);
                var folderPath = picker.Folder.Path;
                RootFolderPath = folderPath;
                Dictionary<string, DeconstructedUser> usersDict = null;
                if (!string.IsNullOrEmpty(folderPath))
                {
                    var result = await ConvertAsync(folderPath, cancellationToken);
                    Channels = result.Channels;
                    usersDict = result.UsersDict;
                }
                else
                {
                    // Handle the case where no folder was selected or the dialog was canceled.
                }

                // Populate UsersDict in Reconstruct before calling ReconstructAsync
                Reconstruct.InitializeUsersDict(usersDict);

                // This checks whether any folder was selected and whether any channels were deconstructed
                if (!string.IsNullOrEmpty(RootFolderPath) && Channels.Count != 0)
                {
                    // Call ReconstructAsync to reconstruct messages for Discord
                    await Reconstruct.ReconstructAsync(Channels, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"ImportJsonAsync() : {ex.Message}\n"); });
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


            foreach (var channelDirectory in channelDirectories)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var channel = new Channel { Name = channelDirectory.Name };
                    var jsonFiles = channelDirectory.GetFiles("*.json");
                    int jsonFileCount = jsonFiles.Length;
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Begin parsing JSON data for {channel.Name} with {jsonFileCount} JSON files...\n"); });

                    foreach (var jsonFile in jsonFiles)
                    {
                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var jsonContent = await File.ReadAllTextAsync(jsonFile.FullName, cancellationToken);
                            var messagesArray = JArray.Parse(jsonContent);

                            foreach (JObject slackMessage in messagesArray.Cast<JObject>())
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var deconstructedMessage = Deconstruct.DeconstructMessage(slackMessage);
                                channel.DeconstructedMessagesList.Add(deconstructedMessage);
                            }
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Exception processing file {jsonFile.Name}: {ex.Message}\n"); });
                        }
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
    }
}
