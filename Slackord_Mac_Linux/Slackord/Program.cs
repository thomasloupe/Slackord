// Slackord2 - Written by Thomas Loupe
// https://github.com/thomasloupe/Slackord2
// https://thomasloupe.com

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using Octokit;
using Microsoft.Extensions.DependencyInjection;
using Discord.Net;
using System.Text.RegularExpressions;

namespace Slackord
{
    internal partial class Slackord : InteractionModuleBase<SocketInteractionContext>
    {
        private const string CurrentVersion = "v2.4.4";
        private DiscordSocketClient _discordClient;
        private string _discordToken;
        private bool _isFileParsed;
        private bool _isParsingNow;
        private IServiceProvider _services;
        private JArray parsed;
        private readonly List<string> Responses = new();
        private readonly List<string> ListOfFilesToParse = new();
        private readonly List<bool> isThreadMessages = new();
        private readonly List<bool> isThreadStart = new();

        static void Main()
        {
            new Slackord().Start();
        }

        public Slackord()
        {
            _isFileParsed = false;
        }

        public async void Start()
        {
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
        }

        private async Task CheckForExistingBotToken()
        {
            if (File.Exists("Token.txt"))
            {
                _discordToken = File.ReadAllText("Token.txt").Trim();
                Console.WriteLine("Found existing token file.");
                if (_discordToken.Length == 0 || string.IsNullOrEmpty(_discordToken))
                {
                    Console.WriteLine("No bot token found. Please enter your bot token: ");
                    _discordToken = Console.ReadLine();
                    File.WriteAllText("Token.txt", _discordToken);
                    await CheckForExistingBotToken();
                }
                else
                {
                    await SelectMenu();
                }
            }
            else
            {
                Console.WriteLine("No bot token found. Please enter your bot token:");
                _discordToken = Console.ReadLine();
                if (_discordToken == null)
                {
                    await CheckForExistingBotToken();
                }
                else
                {
                    File.WriteAllText("Token.txt", _discordToken);
                }
            }
        }

        [STAThread]
        private async Task SelectMenu()
        {
            Console.WriteLine("Please select an option:");
            Console.WriteLine("""
                              1. Import Slack channels to Discord.
                              2. Import Slack messages.
                              """);
            var option = Console.ReadLine();
            switch (option)
            {
                case "1":
                    if (_discordClient.ConnectionState == ConnectionState.Connected)
                    {
                        Console.WriteLine("Using previous Discord client instance...");
                    }
                    else
                    {
                        await MainAsync();
                        Thread.Sleep(3000);
                    }
                    await ImportChannels();
                    break;
                case "2":
                    await ParseJsonFiles();
                    break;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    await SelectMenu();
                    break;
            }
        }

        private async Task ImportChannels()
        {
            if (_discordClient == null || _discordClient.ConnectionState == ConnectionState.Disconnected || _discordClient.ConnectionState == ConnectionState.Disconnecting)
            {
                Console.WriteLine("You must be connected to Discord to create channels!");
                await SelectMenu();
            }

            try
            {
                var json = File.ReadAllText("channels.json");
                var channels = JObject.Parse(json);
            }
            catch (Exception e)
            {
                Console.WriteLine("No channels.json file found. Please import a Slack export first.");
                await SelectMenu();
            }

            Console.WriteLine($"""
                              It is assumed that you have not created any channels with the names of the channels in the JSON file yet. If you have, you will more than likely see duplicate channels.
                              Now is a good time to remove any channels you do not want to create duplicates of. When ready, press "Enter" to continue.
                              """);
            List<string> ChannelsToCreate = new();

            foreach (JObject pair in parsed.Cast<JObject>())
            {
                if (pair.ContainsKey("name"))
                {
                    ChannelsToCreate.Add(pair["name"].ToString());
                }
                await CreateChannelsAsync(ChannelsToCreate).ConfigureAwait(false);
            }
        }

        public async Task CreateChannelsAsync(List<string> _channelsToCreate)
        {
            var guildID = _discordClient.Guilds.FirstOrDefault().Id;

            foreach (var channel in _channelsToCreate)
            {
                try
                {
                    await _discordClient.GetGuild(guildID).CreateTextChannelAsync(channel.ToLower());
                    await Task.Delay(3000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    await SelectMenu();
                }
            }
            Console.WriteLine($"""
                              Channel import completed!
                              The following channels were created:

                              {string.Join(Environment.NewLine, _channelsToCreate)}
                              """);
            await SelectMenu();
        }

        [STAThread]
        private async Task ParseJsonFiles()
        {
            _isParsingNow = true;
            
            Console.WriteLine("Reading JSON files directory...");
            try
            {
                var files = Directory.GetFiles("Files");
                Array.Sort(files);
                if (files.Length == 0)
                {
                    Console.WriteLine("""
                        You haven't placed any JSON files in the Files folder.
                        "Please place your JSON files in the Files folder then press ENTER to continue.
                        """);
                    ConsoleKeyInfo keyPressed = Console.ReadKey(true);
                    if (keyPressed.Key != ConsoleKey.Enter)
                    {
                        await ParseJsonFiles();
                    }
                }
                else
                {
                    Console.WriteLine("Found " + files.Length + " files in the Files folder.");
                    foreach (var file in files)
                    {
                        ListOfFilesToParse.Add(file);
                    }
                }

                foreach (var file in ListOfFilesToParse)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        parsed = JArray.Parse(json);
                        Console.WriteLine("""
                        Begin parsing JSON data...);
                        -----------------------------------------
                        """);
                        string debugResponse;
                        foreach (JObject pair in parsed.Cast<JObject>())
                        {
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
                            
                            if (pair.ContainsKey("files"))
                            {
                                var firstFile = pair["files"][0];
                                List<string> fileKeys = new() { "thumb_1024", "thumb_960", "thumb_720", "thumb_480", "thumb_360", "thumb_160", "thumb_80", "thumb_64", "permalink_public", "permalink", "url_private" };
                                var fileLink = "";
                                foreach (var key in fileKeys)
                                {
                                    try
                                    {
                                        fileLink = firstFile[key].ToString();
                                        if (fileLink.Length > 0)
                                        {
                                            Responses.Add(fileLink + " \n");
                                            break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                        continue;
                                    }
                                }
                                debugResponse = fileLink;
                                Console.WriteLine(debugResponse + "\n");
                            }
                            if (pair.ContainsKey("bot_profile"))
                            {
                                try
                                {
                                    debugResponse = pair["bot_profile"]["name"].ToString() + ": " + pair["text"] + "\n";
                                    Responses.Add(debugResponse);
                                }
                                catch (NullReferenceException)
                                {
                                    try
                                    {
                                        debugResponse = pair["bot_id"].ToString() + ": " + pair["text"] + "\n";
                                        Responses.Add(debugResponse);
                                    }
                                    catch (NullReferenceException)
                                    {
                                        debugResponse = "A bot message was ignored. Please submit an issue on Github for this.";
                                    }
                                }
                                Console.WriteLine(debugResponse + "\n");
                            }
                            if (pair.ContainsKey("user_profile") && pair.ContainsKey("text"))
                            {
                                var rawTimeDate = pair["ts"];
                                var oldDateTime = (double)rawTimeDate;
                                var convertDateTime = ConvertFromUnixTimestampToHumanReadableTime(oldDateTime).ToString("g");
                                var newDateTime = convertDateTime.ToString();
                                var slackUserName = pair["user_profile"]["display_name"]?.ToString();
                                var slackRealName = pair["user_profile"]["real_name"];

                                string slackMessage;
                                if (pair["text"].Contains('|'))
                                {
                                    string preSplit = pair["text"].ToString();
                                    string[] split = preSplit.Split(new char[] { '|' });
                                    string originalText = split[0];
                                    string splitText = split[1];

                                    if (originalText.Contains(splitText))
                                    {
                                        slackMessage = splitText + "\n";
                                    }
                                    else
                                    {
                                        slackMessage = originalText + "\n";
                                    }
                                }
                                else
                                {
                                    slackMessage = pair["text"].ToString();
                                }
                                if (string.IsNullOrEmpty(slackUserName))
                                {
                                    debugResponse = newDateTime + " - " + slackRealName + ": " + slackMessage;
                                    Responses.Add(debugResponse);
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
                                        debugResponse = newDateTime + " - " + slackUserName + ": " + slackMessage + " " + "\n";
                                        Responses.Add(debugResponse);
                                    }
                                }
                                Console.WriteLine(debugResponse + "\n");
                            }
                        }
                        Console.WriteLine($$"""
                        -----------------------------------------
                        Parsing of {{file}} completed successfully!
                        -----------------------------------------
                        """);
                        if (_discordClient != null)
                        {
                            await _discordClient.SetActivityAsync(new Game("awaiting command to import messages...", ActivityType.Watching));
                        }
                        _isFileParsed = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("An error occured while parsing the JSON file. Please try again.");
                        await ParseJsonFiles();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error encountered in input " + e.Message);
            }
            Console.WriteLine("Bot will now attempt to connect to the Discord server...");
            _isParsingNow = false;
            await MainAsync();
        }

        [SlashCommand("slackord", "Posts all parsed Slack JSON messages to the text channel the command came from.")]
        private async Task PostMessages(SocketChannel channel, ulong guildID)
        {
            if (_isParsingNow)
            {
                Console.WriteLine("Slackord is currently parsing one or more JSON files. Please wait until parsing has finished until attempting to post messages.");
                return;
            }

            try
            {
                await _discordClient.SetActivityAsync(new Game("posting messages...", ActivityType.Watching));
                int messageCount = 0;
                
                // TODO: Fix Application did not respond in time error.
                // await DeferAsync();
                if (_isFileParsed)
                {
                    Console.WriteLine("""
                    Beginning transfer of Slack messages to Discord...
                    -----------------------------------------
                    """);

                    SocketThreadChannel threadID = null;
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

                        Regex rx = NewRegex();
                        MatchCollection matches = rx.Matches(messageToSend);
                        if (matches.Count > 0)
                        {
                            foreach (Match match in matches.Cast<Match>())
                            {
                                string matchValue = match.Value;
                                string preSplit = matchValue;
                                string[] split = preSplit.Split(new char[] { '|' });
                                string originalText = split[0];
                                string splitText = split[1];

                                if (originalText.Contains(splitText))
                                {
                                    messageToSend = splitText + "\n";
                                }
                                else
                                {
                                    messageToSend = originalText + "\n";
                                }
                            }
                        }
                        
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
                                        Console.WriteLine("Caught a Slackdump thread reply exception where a JSON entry had thread_ts and wasn't actually a thread start or reply before it excepted. Sending as a normal message...");
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
                            Console.WriteLine("POSTING: " + message);
                        }
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
                                    await threadID.SendMessageAsync(messageToSend).ConfigureAwait(false);
                                }
                                else
                                {
                                    // This exception is hit when a Slackdump export contains a thread_ts in a message that isn't a thread reply.
                                    // We should let the user know and post the message as a normal message, because that's what it is.
                                    Console.WriteLine("Caught a Slackdump thread reply exception where a JSON entry had thread_ts and wasn't actually a thread start or reply before it excepted. Sending as a normal message...");
                                    await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync(messageToSend).ConfigureAwait(false);
                                }
                            }
                            else if (sendAsNormalMessage)
                            {
                                await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync(messageToSend).ConfigureAwait(false);
                            }
                        }
                    }
                    Console.WriteLine("""
                    -----------------------------------------
                    All messages sent to Discord successfully!
                    """);
                    // TODO: Fix Application did not respond in time error.
                    // await FollowupAsync("All messages sent to Discord successfully!", ephemeral: true);
                    await _discordClient.SetActivityAsync(new Game("awaiting parsing of messages.", ActivityType.Watching));
                }
                else if (!_isFileParsed)
                {
                    await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync("Sorry, there's nothing to post because no JSON file was parsed prior to sending this command.").ConfigureAwait(false);
                    Console.WriteLine("Received a command to post messages to Discord, but no JSON file was parsed prior to receiving the command." + "\n");
                }
                await _discordClient.SetActivityAsync(new Game("for the Slackord command...", ActivityType.Listening));
                Responses.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error encountered posting messages: " + ex.Message);
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
                }
                _discordClient = new(_config);
                _services = new ServiceCollection()
                .AddSingleton(_discordClient)
                .BuildServiceProvider();
                _discordClient.Log += DiscordClient_Log;
                await _discordClient.LoginAsync(TokenType.Bot, _discordToken);
                await _discordClient.StartAsync();
                await _discordClient.SetActivityAsync(new Game("awaiting parsing of messages.", ActivityType.Watching));
                _discordClient.Ready += ClientReady;
                _discordClient.SlashCommandExecuted += SlashCommandHandler;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Discord bot task failed with: " + ex.Message);
                await _discordClient.StopAsync();
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.Data.Name.Equals("slackord"))
            {
                var guildID = _discordClient.Guilds.FirstOrDefault().Id;
                var channel = _discordClient.GetChannel((ulong)command.ChannelId);
                await PostMessages(channel, guildID);
            }
        }

        private async Task ClientReady()
        {
            try
            {
                if (_discordClient.Guilds.Count > 0)
                {
                    var guildID = _discordClient.Guilds.FirstOrDefault().Id;
                    var guild = _discordClient.GetGuild(guildID);
                    var guildCommand = new SlashCommandBuilder();
                    guildCommand.WithName("slackord");
                    guildCommand.WithDescription("Posts all parsed Slack JSON messages to the text channel the command came from.");
                    try
                    {
                        await guild.CreateApplicationCommandAsync(guildCommand.Build());
                    }
                    catch (HttpException Ex)
                    {
                        Console.WriteLine("Error creating slash command: " + Ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("Slackord was unable to find any guilds to create a slash command in.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error encountered while creating slash command: " + ex.Message);
            }
        }

        private static DateTime ConvertFromUnixTimestampToHumanReadableTime(double timestamp)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var returnDate = date.AddSeconds(timestamp);
            return returnDate;
        }

        private async Task DiscordClient_Log(LogMessage arg)
        {
            Console.WriteLine(arg.ToString() + ".\n");
            await Task.CompletedTask;
        }

        [GeneratedRegex("(&lt;).*\\|{1}[^|\\n]+(&gt;)", RegexOptions.Compiled | RegexOptions.Singleline)]
        private static partial Regex NewRegex();
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
