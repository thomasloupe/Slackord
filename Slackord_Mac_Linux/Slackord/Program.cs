using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using octo = Octokit;
using Microsoft.Extensions.DependencyInjection;
using Discord.Net;

namespace Slackord
{
    internal class Slackord 
    {
        private const string CurrentVersion = "v2.2.5.1";
        private DiscordSocketClient _discordClient;
        private string _discordToken;
        private bool _isFileParsed;
        private IServiceProvider _services;
        private string fileToRead = string.Empty;
        private JArray parsed;

        static void Main()
        {
            new Slackord().Start();
        }

        public Slackord()
        {
            _isFileParsed = false;
        }

        public void Start()
        {
            AboutSlackord();
            CheckForExistingBotToken();
        }

        public static async void AboutSlackord()
        {
            Console.WriteLine("Slackord " + CurrentVersion + ".\n" +
                "Created by Thomas Loupe." + "\n" +
                "Github: https://github.com/thomasloupe" + "\n" +
                "Twitter: https://twitter.com/acid_rain" + "\n" +
                "Website: https://thomasloupe.com" + "\n");

            Console.WriteLine("Slackord will always be free!\n"
                + "If you'd like to buy me a beer anyway, I won't tell you not to!\n"
                + "You can donate at https://www.paypal.me/thomasloupe\n" + "\n");;
           await CheckForUpdates();
        }

        private static async Task CheckForUpdates()
        {
            var updateCheck = new octo.GitHubClient(new octo.ProductHeaderValue("Slackord2"));
            var releases = await updateCheck.Repository.Release.GetAll("thomasloupe", "Slackord2");
            var latest = releases[0];
            if (CurrentVersion == latest.TagName)
            {
                Console.WriteLine("You have the latest version, " + CurrentVersion + "!");
            }
            else if (CurrentVersion != latest.TagName)
            {
                Console.WriteLine("A new version of Slackord is available!\n"
                    + "Current version: " + CurrentVersion + "\n"
                    + "Latest version: " + latest.TagName + "\n"
                    + "You can get the latest version from the GitHub repository at https://github.com/thomasloupe/Slackord2");
            }
        }

        private void CheckForExistingBotToken()
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
                    CheckForExistingBotToken();
                }
                else
                {
                    SelectFileAndConvertJson();
                }
            }
            else
            {
                Console.WriteLine("No bot token found. Please enter your bot token:");
                _discordToken = Console.ReadLine();
                if (_discordToken == null)
                {
                    CheckForExistingBotToken();
                }
                else
                {
                    File.WriteAllText("Token.txt", _discordToken);
                }
            }
            
        }

        [STAThread]
        private async void SelectFileAndConvertJson()
        {
            Console.WriteLine("Reading JSON files directory...");
            try
            {
                var files = Directory.GetFiles("Files");
                if (files.Length == 0)
                {
                    Console.WriteLine("You haven't placed any JSON files in the Files folder.\n" +
                        "Please place your JSON files in the Files folder then press ENTER to continue.");
                    ConsoleKeyInfo keyPressed = Console.ReadKey(true);
                    if (keyPressed.Key != ConsoleKey.Enter)
                    {
                        SelectFileAndConvertJson();
                    }
                }

                var count = 0;
                foreach (var file in files)
                {
                    Console.WriteLine(count + ": " + file);
                    count++;
                }
                
                Console.WriteLine("Please select a file number you wish to parse and post to Discord: ");
                int index;
                try
                {
                    index = int.Parse(Console.ReadLine());
                    if (index > files.Length || index < 0)
                    {
                        Console.WriteLine("That is not a valid selection, please try again...");
                        SelectFileAndConvertJson();
                    }
                    else
                    {
                        fileToRead = files[index];
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    SelectFileAndConvertJson();
                }

                var json = File.ReadAllText(fileToRead);
                parsed = JArray.Parse(json);
                Console.WriteLine("Begin parsing JSON data..." + "\n");
                Console.WriteLine("-----------------------------------------" + "\n");
                string debugResponse;
                foreach (JObject pair in parsed.Cast<JObject>())
                {
                    if (pair.ContainsKey("files"))
                    {
                        try
                        {
                            debugResponse = pair["text"] + "\n" + pair["files"][0]["thumb_1024"].ToString() + "\n";
                            Console.WriteLine(debugResponse + "\n");
                        }
                        catch (NullReferenceException)
                        {
                            try
                            {
                                debugResponse = pair["text"] + "\n" + pair["files"][0]["url_private"].ToString() + "\n";
                                Console.WriteLine(debugResponse + "\n");
                            }
                            catch (NullReferenceException)
                            {
                                debugResponse = "Skipped a tombstoned file attachement." + "\n";
                                Console.WriteLine(debugResponse);
                            }
                        }
                    }
                    if (pair.ContainsKey("bot_profile"))
                    {
                        try
                        {
                            debugResponse = "Parsed: " + pair["bot_profile"]["name"].ToString() + ": " + pair["text"] + "\n";
                        }
                        catch (NullReferenceException)
                        {
                            try
                            {
                                debugResponse = "Parsed: " + pair["bot_id"].ToString() + ": " + pair["text"] + "\n";
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
                        if (pair["text"].Contains("|"))
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
                            debugResponse = "Parsed: " + newDateTime + " - " + slackRealName + ": " + slackMessage;
                        }
                        else
                        {
                            debugResponse = "Parsed: " + newDateTime + " - " + slackUserName + ": " + slackMessage;
                            if (debugResponse.Length >= 2000)
                            {
                                Console.WriteLine("The following parse is over 2000 characters. Discord does not allow messages over 2000 characters. This message " +
                                    "will be split into multiple posts. The message that will be split is:\n" + debugResponse);
                            }
                            else
                            {
                                debugResponse = "Parsed: " + newDateTime + " - " + slackUserName + ": " + slackMessage + " " + "\n";
                            }
                        }
                        Console.WriteLine(debugResponse + "\n");
                    }
                }
                Console.WriteLine("-----------------------------------------" + "\n");
                Console.WriteLine("Parsing completed successfully!" + "\n");
                if (_discordClient != null)
                {
                    await _discordClient.SetActivityAsync(new Game("awaiting command to import messages...", ActivityType.Watching));
                }
                _isFileParsed = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error encountered in input " + e.Message);
            }
            Console.WriteLine("Bot will now attempt to connect to the Discord server...");
            await MainAsync();
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
            // Let's build a guild command! We're going to need a guild so lets just put that in a variable.
            var guildID = _discordClient.Guilds.FirstOrDefault().Id;
            var guild = _discordClient.GetGuild(guildID);

            // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
            var guildCommand = new SlashCommandBuilder();

            // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
            guildCommand.WithName("slackord");

            // Descriptions can have a max length of 100.
            guildCommand.WithDescription("Posts all parsed Slack JSON messages to the text channel the command came from.");

            try
            {
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
                await guild.CreateApplicationCommandAsync(guildCommand.Build());
            }
            catch (HttpException Ex)
            {
                Console.WriteLine(Ex.Message);
            }
        }

        private static DateTime ConvertFromUnixTimestampToHumanReadableTime(double timestamp)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var returnDate = date.AddSeconds(timestamp);
            return returnDate;
        }

        private Task DiscordClient_Log(LogMessage arg)
        {
            Console.WriteLine(arg.ToString() + "\n");
            return Task.CompletedTask;
        }

        private async Task PostMessages(SocketChannel channel, ulong guildID)
        {
            if (_isFileParsed)
            {
                var files = Directory.GetFiles("Files");
                foreach (var file in files)
                {
                    if (fileToRead == file)
                    {
                        Console.WriteLine("Beginning transfer of Slack messages to Discord..." + "\n" +
                            "-----------------------------------------" + "\n");
                        foreach (JObject pair in parsed.Cast<JObject>())
                        {
                            string slackordResponse;
                            if (pair.ContainsKey("files"))
                            {
                                var rawTimeDate = pair["ts"];
                                var oldDateTime = (double)rawTimeDate;
                                var convertDateTime = ConvertFromUnixTimestampToHumanReadableTime(oldDateTime).ToString("g");
                                var newDateTime = convertDateTime.ToString();
                                try
                                {
                                    slackordResponse = newDateTime + ": " + pair["text"] + "\n" + pair["files"][0]["thumb_1024"].ToString() + "\n";
                                    Console.WriteLine("POSTING: " + slackordResponse);
                                    await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync(slackordResponse).ConfigureAwait(false);
                                }
                                catch (NullReferenceException)
                                {
                                    try
                                    {
                                        slackordResponse = newDateTime + ": " + pair["text"] + "\n" + pair["files"][0]["url_private"].ToString() + "\n";
                                        Console.WriteLine("POSTING: " + slackordResponse);
                                        await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync(slackordResponse).ConfigureAwait(false);
                                    }
                                    catch (NullReferenceException)
                                    {
                                        // Ignore posting this file to Discord.
                                        Console.WriteLine("Skipped a tombstoned file attachement.");
                                    }
                                }
                            }
                            if (pair.ContainsKey("bot_profile"))
                            {
                                try
                                {
                                    slackordResponse = pair["bot_profile"]["name"].ToString() + ": " + pair["text"] + "\n";
                                    Console.WriteLine("POSTING: " + slackordResponse);
                                    await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync(slackordResponse).ConfigureAwait(false);
                                }
                                catch (NullReferenceException)
                                {
                                    try
                                    {
                                        slackordResponse = pair["bot_id"].ToString() + ": " + pair["text"] + "\n";
                                        Console.WriteLine("POSTING: " + slackordResponse);
                                        await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync(slackordResponse).ConfigureAwait(false);
                                    }
                                    catch (NullReferenceException)
                                    {
                                        Console.WriteLine("A bot message was ignored. Please submit an issue on Github for this.");
                                    }
                                }
                            }
                            if (!pair.ContainsKey("user_profile") && !pair.ContainsKey("text"))
                            {
                                Console.WriteLine("A MESSAGE THAT COULDN'T BE SENT WAS SKIPPED HERE." + "\n");
                            }
                            else if (pair.ContainsKey("user_profile") && pair.ContainsKey("text"))
                            {
                                // Can't pass a JToken as a value, so we have to convert it to a string.
                                var rawTimeDate = pair["ts"];
                                var oldDateTime = (double)rawTimeDate;
                                var convertDateTime = ConvertFromUnixTimestampToHumanReadableTime(oldDateTime).ToString("g");
                                var newDateTime = convertDateTime.ToString();
                                var slackUserName = pair["user_profile"]["display_name"].ToString();
                                var slackRealName = pair["user_profile"]["real_name"];
                                var slackMessage = pair["text"] + "\n";

                                if (pair["text"].Contains("|"))
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
                                    slackordResponse = newDateTime + ": " + slackRealName + ": " + slackMessage + " " + "\n";
                                }
                                else
                                {
                                    slackordResponse = newDateTime + ": " + slackUserName + ": " + slackMessage + " " + "\n";
                                }
                                if (slackordResponse.Length >= 2000)
                                {
                                    var responses = slackordResponse.SplitInParts(1800);

                                    Console.WriteLine("SPLITTING AND POSTING: " + slackordResponse);
                                    foreach (var response in responses)
                                    {
                                        slackordResponse = response + " " + "\n";
                                        await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync(slackordResponse).ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("POSTING: " + slackordResponse);
                                    await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync(slackordResponse).ConfigureAwait(false);
                                }
                            }
                        }
                        Console.WriteLine("-----------------------------------------" + "\n" +
                            "All messages sent to Discord successfully!" + "\n");
                        await _discordClient.SetActivityAsync(new Game("awaiting parsing of messages.", ActivityType.Watching));
                    }
                }
            }
            else if (!_isFileParsed)
            {
                await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync("Sorry, there's nothing to post because no JSON file was parsed prior to sending this command.").ConfigureAwait(false);
                Console.WriteLine("Received a command to post messages to Discord, but no JSON file was parsed prior to receiving the command." + "\n");
            }
        }
    }
    static class StringExtensions
    {

        public static IEnumerable<String> SplitInParts(this String s, Int32 partLength)
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
