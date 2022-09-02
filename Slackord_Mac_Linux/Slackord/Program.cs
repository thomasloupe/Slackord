using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using octo = Octokit;
using Microsoft.Extensions.DependencyInjection;

namespace Slackord
{
    internal class Slackord 
    {
        private const string CurrentVersion = "v2.2.1.1";
        private const string CommandTrigger = "!slackord";
        private DiscordSocketClient _discordClient;
        private string _discordToken;
        private bool _isFileParsed;
        private IServiceProvider _services;
        private int _skippedMessages;
        private string fileToRead = String.Empty;
        private JArray parsed;

        static void Main(string[] args)
        {
            new Slackord();
        }

        public Slackord()
        {
            _isFileParsed = false;
            AboutSlackord();
            CheckForExistingBotToken();
        }

        public void AboutSlackord()
        {
            Console.WriteLine("Slackord " + CurrentVersion + ".\n" +
                "Created by Thomas Loupe." + "\n" +
                "Github: https://github.com/thomasloupe" + "\n" +
                "Twitter: https://twitter.com/acid_rain" + "\n" +
                "Website: https://thomasloupe.com" + "\n");

            Console.WriteLine("Slackord will always be free!\n"
                + "If you'd like to buy me a beer anyway, I won't tell you not to!\n"
                + "You can donate at https://www.paypal.me/thomasloupe\n" + "\n");;
            CheckForUpdates();
        }

        private static async void CheckForUpdates()
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
                    Console.ReadLine();
                    SelectFileAndConvertJson();
                }

                var count = 0;
                foreach (var file in files)
                {
                    Console.WriteLine(count + ": " + file);
                    count++;
                }
                
                // Read all files inside Files folder.
                Console.WriteLine("Please select a file number you wish to parse and post to Discord: ");
                var index = int.Parse(Console.ReadLine());
                if (index > files.Length || index < 0)
                {
                    Console.WriteLine("That is not a valid selection, please try again...");
                    SelectFileAndConvertJson();
                }

                fileToRead = files[index];
                var json = File.ReadAllText(fileToRead);
                parsed = JArray.Parse(json);
                var parseFailed = false;
                Console.WriteLine("Begin parsing JSON data..." + "\n");
                Console.WriteLine("-----------------------------------------" + "\n");
                foreach (JObject pair in parsed.Cast<JObject>())
                {
                    var debugResponse = "";
                    if (pair.ContainsKey("files"))
                    {
                        try
                        {
                            debugResponse = "Parsed: " + pair["files"][0]["thumb_1024"].ToString() + "\n";
                        }
                        catch (NullReferenceException)
                        {
                            debugResponse = "Parsed: " + pair["files"][0]["url_private"].ToString() + "\n";
                        }
                        Console.WriteLine(debugResponse + "\n");
                    }
                    if (pair.ContainsKey("user_profile") && pair.ContainsKey("text"))
                    {
                        var rawTimeDate = pair["ts"];
                        var oldDateTime = (double)rawTimeDate;
                        var convertDateTime = ConvertFromUnixTimestampToHumanReadableTime(oldDateTime);
                        var newDateTime = convertDateTime.ToString();
                        var slackUserName = pair["user_profile"]["display_name"]?.ToString();
                        var slackRealName = pair["user_profile"]["real_name"];

                        var slackMessage = "";
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

                        _skippedMessages = 0;
                        if (string.IsNullOrEmpty(slackUserName))
                        {
                            debugResponse = "Parsed: " + newDateTime + " - " + slackRealName + ": " + slackMessage;
                        }
                        else
                        {
                            debugResponse = "Parsed: " + newDateTime + " - " + slackUserName + ": " + slackMessage;
                            if (debugResponse.Length >= 2000)
                            {
                                parseFailed = true;
                                Console.WriteLine("\n" + "PARSING FAILED!" + "\n" +
                                    "A message which contained more than 2000 characters was discovered. Discord does not allow messages over 2000 characters." +
                                    "Please edit your JSON file or posting to Discord will fail." + "\n" +
                                    "For your information, the message was: " + "\n" + "\n");
                                break;
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
                if (parseFailed)
                {
                    Console.WriteLine("FAILED TO PARSE ONE OR MORE MESSAGES! PLEASE SEE THE LOG" + "\n");
                    _isFileParsed = false;
                    if (_discordClient != null)
                    {
                        await _discordClient.SetActivityAsync(new Game("awaiting parsing of messages.", ActivityType.Watching));
                    }
                }
                else
                {
                    if (_skippedMessages > 0)
                    {
                        Console.WriteLine("Parsing completed, but there were " + _skippedMessages + " skipped messages." + "\n");
                    }
                    else
                    {
                        Console.WriteLine("Parsing completed successfully!" + "\n");
                        if (_discordClient != null)
                        {
                            await _discordClient.SetActivityAsync(new Game("awaiting command to import messages...", ActivityType.Watching));
                        }
                        _isFileParsed = true;
                    }

                }
                _skippedMessages = 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error encountered in inpue " + e.Message);
            }
            Console.WriteLine("Bot will now attempt to connect to the Discord server...");
            ConnectBot();
        }

        private async void ConnectBot()
        {
            await MainAsync();
        }

        public async Task MainAsync()
        {
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
            await _discordClient.LoginAsync(TokenType.Bot, _discordToken).ConfigureAwait(false);
            await _discordClient.StartAsync().ConfigureAwait(false);
            await _discordClient.SetActivityAsync(new Game("awaiting parsing of messages.", ActivityType.Watching));
            _discordClient.MessageReceived += MessageReceived;
            await Task.Delay(-1).ConfigureAwait(false);
            Console.ReadLine();
        }

        private static DateTime ConvertFromUnixTimestampToHumanReadableTime(double timestamp)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return date.AddSeconds(timestamp);
        }

        private Task DiscordClient_Log(LogMessage arg)
        {
            Console.WriteLine(arg.ToString() + "\n");
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage message)
        {
            if (_isFileParsed && message.Content.Equals(CommandTrigger, StringComparison.OrdinalIgnoreCase))
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
                                try
                                {
                                    slackordResponse = pair["text"] + "\n" + pair["files"][0]["thumb_1024"].ToString() + "\n";
                                }
                                catch (NullReferenceException)
                                {
                                    slackordResponse = pair["text"] + "\n" + pair["files"][0]["url_private"].ToString() + "\n";
                                }

                                Console.WriteLine("POSTING: " + slackordResponse);
                                await message.Channel.SendMessageAsync(slackordResponse).ConfigureAwait(false);
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
                                var convertDateTime = ConvertFromUnixTimestampToHumanReadableTime(oldDateTime);
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
                                    slackordResponse = newDateTime + " - " + slackRealName + ": " + slackMessage + " " + "\n";
                                }
                                else
                                {
                                    slackordResponse = newDateTime + " - " + slackUserName + ": " + slackMessage + " " + "\n";
                                }
                                if (slackordResponse.Length >= 2000)
                                {
                                    Console.WriteLine("SKIPPING: " + slackordResponse);
                                }
                                else
                                {
                                    Console.WriteLine("POSTING: " + slackordResponse);
                                    await message.Channel.SendMessageAsync(slackordResponse).ConfigureAwait(false);
                                }
                            }
                        }
                        Console.WriteLine("-----------------------------------------" + "\n" +
                            "All messages sent to Discord successfully!" + "\n");
                        await _discordClient.SetActivityAsync(new Game("awaiting parsing of messages.", ActivityType.Watching));
                    }
                }
            }
            else if (!_isFileParsed && message.Content.Equals(CommandTrigger, StringComparison.OrdinalIgnoreCase))
            {
                await message.Channel.SendMessageAsync("Sorry, there's nothing to post because no JSON file was parsed prior to sending this command.").ConfigureAwait(false);
                Console.WriteLine("Received a command to post messages to Discord, but no JSON file was parsed prior to receiving the command." + "\n");
            }
        }
    }
}
