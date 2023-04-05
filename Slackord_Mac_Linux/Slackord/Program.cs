// Slackord2 - Written by Thomas Loupe
// Repo   : https://github.com/thomasloupe/Slackord2
// Website: https://thomasloupe.com
// Twitter: https://twitter.com/acid_rain
// PayPal : https://paypal.me/thomasloupe

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Octokit;
using System.Globalization;

namespace Slackord
{
    internal class Slackord : InteractionModuleBase<SocketInteractionContext>
    {
        private const string CurrentVersion = "v2.4.9";
        private DiscordSocketClient? _discordClient;
        private string? _discordToken;
        private bool _isFileParsed;
        private bool _isParsingNow;
        public IServiceProvider? _services;
        private JArray? parsed;
        private List<string> ListOfFilesToParse = new();
        private readonly List<string> Responses = new();
        private readonly List<bool> isThreadMessages = new();
        private readonly List<bool> isThreadStart = new();
        private readonly TaskCompletionSource<bool> _botReadyTcs = new();

        static async Task Main()
        {
            var slackord = new Slackord();
            await slackord.Initialize();
        }

        public async Task Initialize()
        {
            _isFileParsed = false;
            await AboutUpdate();
            await CheckForExistingBotToken();
        }

        public static async Task AboutUpdate()
        {
            Console.WriteLine($"""
                Slackord {CurrentVersion}
                Created by Thomas Loupe.
                Github: https://github.com/thomasloupe
                Twitter: https://twitter.com/acid_rain
                Website: https://thomasloupe.com
                
                """);

            Console.WriteLine("""
                Slackord will always be free!
                If you'd like to buy me a beer anyway, I won't tell you not to!
                You can donate at https://www.paypal.me/thomasloupe
                """);

            var client = new GitHubClient(new ProductHeaderValue("Slackord2"));
            var releases = client.Repository.Release.GetAll("thomasloupe", "Slackord2");
            var latest = releases;
            var latestVersion = latest.Result[0].TagName;

            if (CurrentVersion == latestVersion)
            {
                Console.WriteLine("You have the latest version, " + CurrentVersion + "!");
            }
            else if (CurrentVersion != latestVersion)
            {
                Console.WriteLine("""
                    You are running an outdated version of Slackord!
                    Current Version: {0}
                    Latest Version: {1}
                    You can download the latest version at https://github.com/thomasloupe/Slackord2/releases/tag/{1}
                                  
                    """, CurrentVersion, latestVersion);
            }
            await Task.CompletedTask;
        }

        private async Task CheckForExistingBotToken()
        {
            if (File.Exists("Token.txt"))
            {
                _discordToken = File.ReadAllText("Token.txt").Trim();
                Console.WriteLine("""

                                 Found an existing bot token.

                                 """);
                if (_discordToken.Length == 0 || string.IsNullOrEmpty(_discordToken))
                {
                    Console.WriteLine("""

                        No bot token found. Please enter your token:

                        """);
                    _discordToken = Console.ReadLine()?.Trim();
                    File.WriteAllText("Token.txt", _discordToken);
                    await CheckForExistingBotToken();
                }
            }
            else
            {
                Console.WriteLine("""

                    No bot token found. Please enter your token:

                    """);
                _discordToken = Console.ReadLine();
                if (string.IsNullOrEmpty(_discordToken))
                {
                    await CheckForExistingBotToken();
                }
                else
                {
                    File.WriteAllText("Token.txt", _discordToken);
                }
            }
            await MainAsync();
        }

        private async Task ImportChannels(DiscordSocketClient? _discordClient)
        {
            if (_discordClient == null || _discordClient.ConnectionState == ConnectionState.Disconnected || _discordClient.ConnectionState == ConnectionState.Disconnecting)
            {
                Console.WriteLine("You must be connected to Discord to create channels!");
                await SelectMenu(_discordClient);
            }

            List<string> ChannelsToCreate = new();

            try
            {
                var json = File.ReadAllText("channels.json");
                JArray? channels = null;

                try
                {
                    channels = JArray.Parse(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing channels.json: {ex.Message}");
                }

                if (channels != null)
                {
                    foreach (JObject obj in channels.Cast<JObject>())
                    {
                        if (obj.ContainsKey("name"))
                        {
                            var name = obj["name"];
                            if (name != null)
                            {
                                ChannelsToCreate.Add(name.ToString());
                            }
                        }
                    }
                }

                Console.WriteLine($@"
                    It is assumed that you have not created any channels with the names of the channels in the JSON file yet. 
                    If you have, you will more than likely see duplicate channels.
                    Now is a good time to remove any channels you do not want to create duplicates of.
                    Please assign the administrator role to your bot at this time so it can create the channels.
                    When ready, press any key to continue.
                    ");
                Console.ReadKey(true);
                await CreateChannelsAsync(ChannelsToCreate, _discordClient).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                await SelectMenu(_discordClient);
            }
        }

        private async Task SelectMenu(DiscordSocketClient? _discordClient)
        {
            Console.WriteLine("\nPlease select an option:");
            Console.Write("""
                1. Import Slack channels to Discord.
                2. Import Slack messages.
                
                Selection: 
                """);
            string? input = Console.ReadLine();
            if (input != null)
            {
                try
                {
                    int option = int.Parse(input);
                    if (option == 1)
                    {
                        if (_discordClient != null)
                        {
                            await ImportChannels(_discordClient);
                        }
                        else
                        {
                            Console.WriteLine("Error: Discord client is null");
                        }
                    }
                    if (option == 2)
                    {
                        ParseJsonFiles();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Not a valid option...\n" + ex.Message);
                    if (_discordClient != null)
                    {
                        await SelectMenu(_discordClient);
                    }
                }
            }
        }

        public async Task CreateChannelsAsync(List<string> _channelsToCreate, DiscordSocketClient? _discordClient)
        {
            if (_discordClient == null)
            {
                Console.WriteLine("Error: Discord client is null");
                return;
            }

            var guild = _discordClient.Guilds.FirstOrDefault();
            if (guild != null)
            {
                var guildID = guild.Id;
                foreach (var channel in _channelsToCreate)
                {
                    try
                    {
                        Console.Write($"Creating channel '{channel.ToLower()}'...");
                        await _discordClient.GetGuild(guildID).CreateTextChannelAsync(channel.ToLower());
                        Console.WriteLine("Success!");
                        await Task.Delay(5000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating channel '{channel.ToLower()}': {ex.Message}");
                        await SelectMenu(_discordClient);
                    }
                }
            }
            else
            {
                Console.WriteLine("Error: Discord client is not connected to any guilds");
            }
        }

        [STAThread]
        private void ParseJsonFiles()
        {
            _isParsingNow = true;

            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var files = Directory.EnumerateFiles(appDirectory + "Files", "*.*", SearchOption.TopDirectoryOnly)
                        .Where(s => s.EndsWith(".JSON") || s.EndsWith(".json"));

            foreach (var file in files)
            {
                ListOfFilesToParse.Add(file);
            }

            ListOfFilesToParse = ListOfFilesToParse
                .OrderBy(file => DateTime.ParseExact(
                    Path.GetFileNameWithoutExtension(file),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture))
                .ToList();

            foreach (var file in ListOfFilesToParse)
            {
                Console.WriteLine($"""

                    -----------------------------------------
                    Begin parsing JSON data for {file}...
                    -----------------------------------------
                    
                    """);

                try
                {
                    var json = File.ReadAllText(file);
                    parsed = JArray.Parse(json);
                    string debugResponse;
                    foreach (JObject pair in parsed.Cast<JObject>())
                    {
                        var rawTimeDate = pair["ts"];
                        if (rawTimeDate == null) continue;
                        double oldDateTime = (double)rawTimeDate;
                        string convertDateTime = ConvertFromUnixTimestampToHumanReadableTime(oldDateTime).ToString("g");
                        string newDateTime = convertDateTime.ToString();

                        if (pair.ContainsKey("reply_count") && pair.ContainsKey("thread_ts"))
                        {
                            isThreadStart.Add(true);
                            isThreadMessages.Add(false);
                        }
                        else if (pair.ContainsKey("thread_ts") && !pair.ContainsKey("reply_count"))
                        {
                            isThreadStart.Add(false);
                            isThreadMessages.Add(true);
                        }
                        else
                        {
                            isThreadStart.Add(false);
                            isThreadMessages.Add(false);
                        }

                        if (pair.ContainsKey("text") && !pair.ContainsKey("bot_profile"))
                        {
                            string slackUserName = "";
                            string slackRealName = "";

                            if (pair.ContainsKey("user_profile"))
                            {
                                var userProfile = pair["user_profile"];
                                slackUserName = userProfile?["display_name"]?.ToString() ?? "";
                                slackRealName = userProfile?["real_name"]?.ToString() ?? "";
                            }

                            string slackMessage = pair.ContainsKey("text") ? pair["text"]?.ToString() ?? "" : "";

                            slackMessage = DeDupeURLs(slackMessage);

                            if (string.IsNullOrEmpty(slackUserName))
                            {
                                if (string.IsNullOrEmpty(slackRealName))
                                {
                                    debugResponse = newDateTime + " - " + slackMessage;
                                    Responses.Add(debugResponse);
                                }
                                else
                                {
                                    debugResponse = newDateTime + " - " + slackRealName + ": " + slackMessage;
                                    Responses.Add(debugResponse);
                                }
                            }
                            else
                            {
                                debugResponse = newDateTime + " - " + slackUserName + ": " + slackMessage;
                                if (debugResponse.Length >= 2000)
                                {
                                    Console.WriteLine($"""
                                    The following parse is over 2000 characters. Discord does not allow messages over 2000 characters.
                                    This message will be split into multiple posts. The message that will be split is: {debugResponse}
                                    """);
                                }
                                else
                                {
                                    debugResponse = newDateTime + " - " + slackUserName + ": " + slackMessage;
                                    Responses.Add(debugResponse);
                                }
                            }
                            Console.WriteLine(debugResponse + "\n");
                        }

                        if (pair.ContainsKey("files"))
                        {
                            var filesArray = pair["files"];
                            if (filesArray == null) continue;
                            var firstFile = filesArray[0];
                            if (firstFile == null) continue;
                            List<string> fileKeys = new();

                            var fileTypeToken = firstFile.SelectToken("filetype");
                            if (fileTypeToken != null)
                            {
                                string fileType = fileTypeToken.ToString();
                                if (fileType == "mp4")
                                {
                                    fileKeys = new List<string> { "permalink" };
                                }
                                else
                                {
                                    fileKeys = new List<string> { "thumb_1024", "thumb_960", "thumb_720", "thumb_480", "thumb_360", "thumb_160", "thumb_80", "thumb_64", "thumb_video", "permalink_public", "permalink", "url_private" };
                                }
                            }
                            else
                            {
                                continue;
                            }

                            var fileLink = "";
                            bool foundValidKey = false;
                            foreach (var key in fileKeys)
                            {
                                try
                                {
                                    var keyValue = firstFile[key];
                                    if (keyValue == null) continue;
                                    fileLink = keyValue.ToString();
                                    if (!string.IsNullOrEmpty(fileLink))
                                    {
                                        Responses.Add(fileLink + " \n");
                                        foundValidKey = true;
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Exception: " + ex.Source + "\n\n" + ex.Message + "\n\n" + ex.StackTrace);
                                    continue;
                                }
                            }

                            if (!foundValidKey)
                            {
                                continue;
                            }

                            debugResponse = fileLink;
                            Console.WriteLine(debugResponse + "\n");
                        }

                        if (pair.ContainsKey("bot_profile"))
                        {
                            try
                            {
                                var nameToken = pair["bot_profile"]?["name"];
                                string name = nameToken?.ToString() ?? (pair["bot_id"]?.ToString() ?? "");
                                debugResponse = name + ": " + pair["text"] + "\n";
                                Responses.Add(debugResponse);
                            }
                            catch (Exception)
                            {
                                debugResponse = "A bot message was ignored. Please submit an issue on Github for this.";
                            }
                            Console.WriteLine(debugResponse + "\n");
                        }
                    }
                    Console.WriteLine($"""
                        -----------------------------------------
                        Parsing of {file} completed successfully!
                        -----------------------------------------

                        """);
                    _isFileParsed = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                _discordClient?.SetActivityAsync(new Game("- awaiting command to import messages...", ActivityType.Watching));
            }
            _isParsingNow = false;
        }

        private static string DeDupeURLs(string input)
        {
            input = input.Replace("<", "").Replace(">", "");

            string[] parts = input.Split('|');

            if (parts.Length == 2)
            {
                if (Uri.TryCreate(parts[0], UriKind.Absolute, out Uri? uri1) &&
                    Uri.TryCreate(parts[1], UriKind.Absolute, out Uri? uri2))
                {
                    if (uri1 != null && uri2 != null && uri1.GetLeftPart(UriPartial.Path) == uri2.GetLeftPart(UriPartial.Path))
                    {
                        input = input.Replace(parts[1] + "|", "");
                    }
                }
            }

            string[] parts2 = input.Split('|').Distinct().ToArray();
            input = string.Join("|", parts2);

            return input;
        }

        [SlashCommand("slackord", "Posts all parsed Slack JSON messages to the text channel the command came from.")]
        public async Task PostMessagesToDiscord(SocketChannel channel, ulong guildID, SocketInteraction interaction)
        {
            await interaction.DeferAsync();
            if (_isParsingNow)
            {
                Console.WriteLine("Slackord is currently parsing one or more JSON files. Please wait until parsing has finished until attempting to post messages.");
                return;
            }

            try
            {
                if (_discordClient == null)
                {
                    Console.WriteLine("Discord client is null.");
                    return;
                }
                await _discordClient.SetActivityAsync(new Game("messages...", ActivityType.Streaming));
                int messageCount = 0;

                if (_isFileParsed)
                {
                    Console.WriteLine("""

                        Beginning transfer of Slack messages to Discord...
                        -----------------------------------------
                        
                        """);

                    SocketThreadChannel? threadID = null;
                    foreach (string message in Responses)
                    {
                        bool sendAsThread = false;
                        bool sendAsThreadReply = false;
                        bool sendAsNormalMessage = false;

                        string messageToSend = message;
                        bool wasSplit = false;

                        if (isThreadStart[messageCount] == true)
                        {
                            sendAsThread = true;
                        }
                        else if (isThreadStart[messageCount] == false && isThreadMessages[messageCount] == true)
                        {
                            sendAsThreadReply = true;
                        }
                        else
                        {
                            sendAsNormalMessage = true;
                        }
                        messageCount += 1;

                        if (message.Length >= 2000)
                        {
                            var responses = messageToSend.SplitInParts(1800);

                            Console.WriteLine("SPLITTING AND POSTING: " + messageToSend);

                            foreach (var response in responses)
                            {
                                messageToSend = response + " " + "\n";
                                if (sendAsThread)
                                {
                                    await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync(messageToSend).ConfigureAwait(false);
                                    var messages = await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).GetMessagesAsync(1).FlattenAsync();
                                    threadID = await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).CreateThreadAsync("Slackord Thread", ThreadType.PublicThread, ThreadArchiveDuration.OneDay, messages.First());
                                }
                                else if (sendAsThreadReply)
                                {
                                    if (threadID is not null)
                                    {
                                        await threadID.SendMessageAsync(messageToSend).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        // This exception is hit when a Slackdump export contains a thread_ts in a message that isn't a thread reply.
                                        // We should let the user know and post the message as a normal message, because that's what it is.
                                        Console.WriteLine("Caught a Slackdump thread reply exception where a JSON entry had thread_ts and wasn't actually a thread start or reply before it excepted." +
                                                         "Sending as a normal message...");
                                        await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync(messageToSend).ConfigureAwait(false);
                                    }
                                }
                                else if (sendAsNormalMessage)
                                {
                                    await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync(messageToSend).ConfigureAwait(false);
                                }
                            }
                            wasSplit = true;
                        }
                        else
                        {
                            Console.WriteLine($"""
                            POSTING: {message}
                            """);

                            if (!wasSplit)
                            {
                                if (sendAsThread)
                                {
                                    await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync(messageToSend).ConfigureAwait(false);
                                    var messages = await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).GetMessagesAsync(1).FlattenAsync();
                                    threadID = await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).CreateThreadAsync("Slackord Thread", ThreadType.PublicThread, ThreadArchiveDuration.OneDay, messages.First());
                                }
                                else if (sendAsThreadReply)
                                {

                                    if (threadID is not null)
                                    {
                                        await threadID.SendMessageAsync(messageToSend);
                                    }
                                    else
                                    {
                                        // This exception is hit when a Slackdump export contains a thread_ts in a message that isn't a thread reply.
                                        // We should let the user know and post the message as a normal message, because that's what it is.
                                        Console.WriteLine("""
                                            Caught a Slackdump thread reply exception where a JSON entry had thread_ts and wasn't actually a thread start or reply before it excepted.
                                            Sending as a normal message...");
                                            """);
                                        await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync(messageToSend);
                                    }
                                }
                                else if (sendAsNormalMessage)
                                {
                                    await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync(messageToSend).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    Console.WriteLine(new Action(() =>
                    {
                        Console.WriteLine("""
                        -----------------------------------------
                        All messages sent to Discord successfully!
                        """);
                    }));
                    await interaction.FollowupAsync("All messages sent to Discord successfully!", ephemeral: true);
                    await _discordClient.SetActivityAsync(new Game("- awaiting parsing of messages.", ActivityType.Watching));
                }
                else
                {
                    await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync("Sorry, there's nothing to post because no JSON file was parsed prior to sending this command.").ConfigureAwait(false);
                    Console.WriteLine("Received a command to post messages to Discord, but no JSON file was parsed prior to receiving the command." + "\n");
                }
                Responses.Clear();
                await _discordClient.SetActivityAsync(new Game("for the Slackord command...", ActivityType.Listening));
            }
            catch (Exception ex)
            {
                var exception = ex.GetType().ToString();
                if (exception.Equals("Discord.Net.HttpException"))
                {
                    switch (ex.Message)
                    {
                        case "The server responded with error 50006: Cannot send an empty message":
                            Console.WriteLine("The server responded with error 50006: Cannot send an empty message");
                            break;
                    }
                }
            }
        }

        public async Task MainAsync()
        {
            try
            {
                var thread = new Thread(() => { while (true) Thread.Sleep(5000); }); thread.Start();
                _discordClient = new DiscordSocketClient();
                DiscordSocketConfig _config = new();
                {
                    _config.GatewayIntents = GatewayIntents.DirectMessages | GatewayIntents.GuildMessages | GatewayIntents.Guilds;
                    _config.LogLevel = LogSeverity.Verbose;
                }
                _discordClient = new(_config);
                _services = new ServiceCollection()
                .AddSingleton(_discordClient)
                .BuildServiceProvider();
                _discordClient.Log += SlackordDiscordClient_Log;
                await _discordClient.LoginAsync(TokenType.Bot, _discordToken);
                await _discordClient.StartAsync();

                _discordClient.Ready += () =>
                {
                    _botReadyTcs.SetResult(true);
                    return Task.CompletedTask;
                };

                await _botReadyTcs.Task;

                await _discordClient.SetActivityAsync(new Game("- awaiting parsing of messages.", ActivityType.Watching));
                _discordClient.SlashCommandExecuted += SlashCommandHandler;
                await SelectMenu(_discordClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Discord bot task failed with: " + ex.Source + "\n\n" + ex.Message + "\n\n" + ex.StackTrace);
                _discordClient?.StopAsync();
            }
            await Task.CompletedTask;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (string.Equals(command.Data.Name, "slackord", StringComparison.OrdinalIgnoreCase))
            {
                if (_discordClient is null)
                {
                    Console.WriteLine("Discord client is not initialized.");
                    return;
                }

                var guild = _discordClient.Guilds.FirstOrDefault();
                if (guild is null)
                {
                    Console.WriteLine("Could not find any guilds in the client.");
                    return;
                }

                var channel = _discordClient.GetChannel((ulong)command.ChannelId)! ?? throw new Exception($"Could not find channel with ID {command.ChannelId}.");
                var interaction = command;
                await PostMessagesToDiscord(channel, guild.Id, interaction);
            }
        }

        private static Task SlackordDiscordClient_Log(LogMessage logMessage)
        {
            Console.WriteLine($"[{logMessage.Severity}] {logMessage.Source}: {logMessage.Message}");
            return Task.CompletedTask;
        }

        private static DateTime ConvertFromUnixTimestampToHumanReadableTime(double timestamp)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var returnDate = date.AddSeconds(timestamp);
            return returnDate;
        }
    }

    static class StringExtensions
    {
        public static IEnumerable<string> SplitInParts(this string s, int partLength)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (partLength <= 0)
                throw new ArgumentException("Invalid char length specified.", nameof(partLength));

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }
    }
}
