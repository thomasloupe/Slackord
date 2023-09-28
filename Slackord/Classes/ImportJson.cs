namespace Slackord.Classes
{
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json.Linq;
    using System.Threading.Tasks;
    using System.Threading;
    using CommunityToolkit.Maui.Storage;
    using MenuApp;

    public class ImportJson
    {
        public static List<Channel> Channels { get; set; } = new List<Channel>();

        public static async Task ImportJsonAsync(CancellationToken cancellationToken)
        {
            try
            {
                var picker = await FolderPicker.Default.PickAsync(cancellationToken);
                var folderPath = picker.Folder.Path;
                if (!string.IsNullOrEmpty(folderPath))
                {
                    Channels = await ConvertAsync(folderPath, cancellationToken);
                }
                else
                {
                    // Handle the case where no folder was selected or the dialog was canceled.
                }
            }
            catch (OperationCanceledException)
            {
                // Handle the cancellation.
            }
        }

        public static async Task<List<Channel>> ConvertAsync(string folderPath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var channels = new List<Channel>();
            var directoryInfo = new DirectoryInfo(folderPath);
            var channelDirectories = directoryInfo.GetDirectories();

            foreach (var channelDirectory in channelDirectories)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var channel = new Channel { Name = channelDirectory.Name };
                    var jsonFiles = channelDirectory.GetFiles("*.json");
                    int jsonFileCount = jsonFiles.Length;

                    ApplicationWindow.WriteToDebugWindow($"Importing channel {channel.Name} with {jsonFileCount} JSON files.\n");

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

                                var discordMessage = MessageBuilder.BuildMessage(slackMessage);
                                channel.Messages.Add(discordMessage);
                            }
                        }
                        catch (Exception ex)
                        {
                            ApplicationWindow.WriteToDebugWindow($"Exception processing file {jsonFile.Name}: {ex.Message}\n");
                        }
                    }

                    channels.Add(channel);
                    ApplicationWindow.WriteToDebugWindow($"Completed importing channel {channel.Name}.\n");
                }
                catch (Exception ex)
                {
                    ApplicationWindow.WriteToDebugWindow($"Exception processing channel {channelDirectory.Name}: {ex.Message}\n");
                }
            }

            return channels;
        }

        public class Channel
        {
            public string Name { get; set; }
            public List<Message> Messages { get; set; } = new List<Message>();
        }
    }
}
