// Slackord2 - Written by Thomas Loupe
// Repo   : https://github.com/thomasloupe/Slackord2
// Website: https://thomasloupe.com
// Twitter: https://twitter.com/acid_rain
// PayPal : https://paypal.me/thomasloupe

using Application = System.Windows.Forms.Application;
using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Label = System.Windows.Forms.Label;
using Microsoft.Extensions.DependencyInjection;
using MaterialSkin;
using MaterialSkin.Controls;
using Newtonsoft.Json.Linq;
using Octokit;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Slackord
{
    public partial class Slackord : MaterialForm
    {
        private const string CurrentVersion = "v2.5";
        public DiscordSocketClient _discordClient;
        private string _discordToken;
        private GitHubClient _octoClient;
        public bool _isFileParsed;
        private bool _isParsingNow;
        public bool _showDebugOutput = false;
        public IServiceProvider _services;
        public JArray parsed;
        public Dictionary<string, List<string>> JsonFilesDict { get; private set; } = new Dictionary<string, List<string>>();
        private readonly List<string> Responses = new();
        private readonly List<bool> isThreadMessages = new();
        private readonly List<bool> isThreadStart = new();
        int totalMessageCount = 0;
        int currentMessageCount = 0;

        public Slackord()
        {
            InitializeComponent();
            SetWindowSizeAndLocation();
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.Orange600, Primary.DeepOrange700, Primary.Amber500, Accent.Amber700, TextShade.BLACK);
            _isFileParsed = false;
            progressBar1.Enabled = false;
            CheckForExistingBotToken();
        }

        private void SetWindowSizeAndLocation()
        {
            Location = Properties.Settings.Default.FormLocation;
            Height = Properties.Settings.Default.FormHeight;
            Width = Properties.Settings.Default.FormWidth;
            FormClosing += SaveSettingsEventHandler;
            StartPosition = FormStartPosition.Manual;
        }

        private void CheckForExistingBotToken()
        {
            DisableBothBotConnectionButtons();
            _discordToken = Properties.Settings.Default.SlackordBotToken.Trim();
            if (Properties.Settings.Default.FirstRun)
            {
                richTextBox1.AppendText("Welcome to Slackord 2!" + "\n");
            }
            else if (string.IsNullOrEmpty(_discordToken) || string.IsNullOrEmpty(Properties.Settings.Default.SlackordBotToken))
            {
                richTextBox1.AppendText("""
                Slackord 2 tried to automatically load your last bot token but wasn't successful.
                The token is not long enough or the token value is empty. Please enter a new token.
                """);
            }
            else
            {
                richTextBox1.AppendText("Slackord 2 found a previously entered bot token and automatically applied it! Bot connection is now enabled." + "\n");
                EnableBotConnectionMenuItem();
            }
        }

        private void ImportJSONFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                Responses.Clear();
                JsonFilesDict.Clear();

                totalMessageCount = 0;
                currentMessageCount = 0;
                progressBar1.Enabled = false;

                var subDirectories = Directory.GetDirectories(fbd.SelectedPath);
                int folderCount = subDirectories.Length;
                int fileCount = 0;

                foreach (var subDir in subDirectories)
                {
                    var folderName = Path.GetFileName(subDir);
                    var files = Directory.EnumerateFiles(subDir, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(s => s.EndsWith(".JSON") || s.EndsWith(".json"));

                    List<string> fileList = new();
                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        if (DateTime.TryParseExact(fileName, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fileDate))
                        {
                            fileList.Add(file);
                            fileCount++;
                        }
                    }
                    if (fileList.Count > 0)
                    {
                        JsonFilesDict[folderName] = fileList;
                    }
                }

                MessageBox.Show($"Found {fileCount} JSON files in {folderCount} folders.", "Message");

                foreach (var folder in JsonFilesDict.Keys)
                {
                    ParseJsonFiles(JsonFilesDict[folder], folder);
                }
            }
        }

        private void ParseJsonFiles(List<string> files, string channelName)
        {
            _isParsingNow = true;
            richTextBox1.AppendText($"""
            Begin parsing JSON data for {channelName}...
            -----------------------------------------

            """);
            try
            {
                string debugResponse;
                string currentFile = "";
                foreach (string file in files)
                {
                    currentFile = Path.GetFileNameWithoutExtension(file);
                    var json = File.ReadAllText(file);
                    parsed = JArray.Parse(json);
                    foreach (JObject pair in parsed.Cast<JObject>())
                    {
                        var rawTimeDate = pair["ts"];
                        double oldDateTime = (double)rawTimeDate;
                        string convertDateTime = ConvertFromUnixTimestampToHumanReadableTime(oldDateTime).ToString("g");
                        string newDateTime = convertDateTime.ToString();

                        // JSON message thread handling.
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

                        // JSON message parsing.
                        if (pair.ContainsKey("text") && !pair.ContainsKey("bot_profile"))
                        {
                            string slackUserName = "";
                            string slackRealName = "";

                            if (pair.ContainsKey("user_profile"))
                            {
                                slackUserName = pair["user_profile"]["display_name"].ToString();
                                slackRealName = pair["user_profile"]["real_name"].ToString();
                            }

                            string slackMessage = pair["text"].ToString();

                            slackMessage = DeDupeURLs(slackMessage);

                            if (string.IsNullOrEmpty(slackUserName))
                            {
                                if (string.IsNullOrEmpty(slackRealName))
                                {
                                    debugResponse = newDateTime + " - " + slackMessage;
                                    Responses.Add(debugResponse);
                                    totalMessageCount++;
                                }
                                else
                                {
                                    debugResponse = newDateTime + " - " + slackRealName + ": " + slackMessage;
                                    Responses.Add(debugResponse);
                                    totalMessageCount++;
                                }
                            }
                            else
                            {
                                debugResponse = newDateTime + " - " + slackUserName + ": " + slackMessage;
                                if (debugResponse.Length >= 2000)
                                {
                                    richTextBox1.AppendText($@"
                                    The following parse is over 2000 characters. Discord does not allow messages over 2000 characters.
                                    This message will be split into multiple posts. The message that will be split is: {debugResponse}
                                    ");
                                }
                                else
                                {
                                    debugResponse = newDateTime + " - " + slackUserName + ": " + slackMessage;
                                    Responses.Add(debugResponse);
                                    totalMessageCount++;
                                }
                            }
                            richTextBox1.AppendText(debugResponse + "\n");
                        }

                        if (pair.ContainsKey("files") && pair["files"] is JArray filesArray && filesArray.Count > 0)
                        {
                            var fileLink = filesArray[0]["url_private"]?.ToString();

                            if (!string.IsNullOrEmpty(fileLink))
                            {
                                debugResponse = fileLink;
                                richTextBox1.AppendText(debugResponse + "\n");
                            }
                        }

                        if (pair.ContainsKey("bot_profile"))
                        {
                            try
                            {
                                debugResponse = pair["bot_profile"]["name"].ToString() + ": " + pair["text"] + "\n";
                                Responses.Add(debugResponse);
                                totalMessageCount++;
                            }
                            catch (NullReferenceException)
                            {
                                try
                                {
                                    debugResponse = pair["bot_id"].ToString() + ": " + pair["text"] + "\n";
                                    Responses.Add(debugResponse);
                                    totalMessageCount++;
                                }
                                catch (NullReferenceException)
                                {
                                    debugResponse = "A bot message was ignored. Please submit an issue on Github for this.";
                                }
                            }
                            richTextBox1.AppendText(debugResponse + "\n");
                        }
                        UpdateDebugWindowView();
                    }
                }
                richTextBox1.AppendText($"""
                -----------------------------------------
                Parsing of {currentFile} completed successfully!
                -----------------------------------------

                """);

                _isFileParsed = true;
                richTextBox1.ForeColor = System.Drawing.Color.DarkGreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            _discordClient?.SetActivityAsync(new Game("for the Slackord command...", ActivityType.Watching));
            _isParsingNow = false;
        }

        [SlashCommand("slackord", "Posts all parsed Slack JSON messages to the text channel the command came from.")]
        public async Task PostMessagesToDiscord(ulong guildID, SocketInteraction interaction)
        {
            if (_isParsingNow)
            {
                MessageBox.Show("Slackord is currently parsing one or more JSON files. Please wait until parsing has finished until attempting to post messages.");
                await Task.CompletedTask;
            }

            progressBar1.Invoke(() =>
            {
                progressBar1.Enabled = true;
                progressBar1.Maximum = totalMessageCount;
                progressBar1.Value = 0;
            });

            await interaction.DeferAsync();

            foreach (var channelName in JsonFilesDict.Keys)
            {
                var createdChannel = await _discordClient.GetGuild(guildID).CreateTextChannelAsync(channelName);
                var createdChannelId = createdChannel.Id;

                MessageBox.Show($"{channelName}: {createdChannelId}");

                richTextBox1.Invoke(new Action(() =>
                {
                    richTextBox1.AppendText($"Created {channelName} on Discord with ID: {createdChannelId}.\n");
                }));

                try
                {
                    await _discordClient.SetActivityAsync(new Game("messages...", ActivityType.Streaming));

                    int messageCount = 0;

                    if (JsonFilesDict.TryGetValue(channelName, out var messages))
                    {
                        richTextBox1.Invoke(new Action(() =>
                        {
                            richTextBox1.AppendText($@"
                            Beginning transfer of Slack messages to Discord for {channelName}...
                            -----------------------------------------
                            
                            ");
                        }));

                        SocketThreadChannel threadID = null;

                        foreach (string message in messages)
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

                                richTextBox1.Invoke(new Action(() =>
                                {
                                    richTextBox1.AppendText("SPLITTING AND POSTING: " + messageToSend);
                                }));

                                foreach (var response in responses)
                                {
                                    messageToSend = response + " " + "\n";

                                    if (sendAsThread)
                                    {
                                        if (_discordClient.GetChannel(createdChannelId) is SocketTextChannel textChannel)
                                        {
                                            await textChannel.SendMessageAsync(messageToSend).ConfigureAwait(false);
                                            var latestMessages = await textChannel.GetMessagesAsync(1).FlattenAsync();
                                            threadID = await textChannel.CreateThreadAsync("Slackord Thread", ThreadType.PublicThread, ThreadArchiveDuration.OneDay, latestMessages.First());
                                        }
                                    }
                                    else if (sendAsThreadReply)
                                    {
                                        if (threadID is not null)
                                        {
                                            await threadID.SendMessageAsync(messageToSend).ConfigureAwait(false);
                                        }
                                        else
                                        {
                                            richTextBox1.Invoke(new MethodInvoker(delegate ()
                                            {
                                                richTextBox1.AppendText("Caught a Slackdump thread reply exception where a JSON entry had thread_ts and wasn't actually a thread start or reply before it excepted. Sending as a normal message...");
                                            }));
                                            await _discordClient.GetGuild(guildID).GetTextChannel(createdChannelId).SendMessageAsync(messageToSend).ConfigureAwait(false);
                                        }
                                    }
                                    else if (sendAsNormalMessage)
                                    {
                                        await _discordClient.GetGuild(guildID).GetTextChannel(createdChannelId).SendMessageAsync(messageToSend).ConfigureAwait(false);
                                    }
                                    UpdateProgressBar();
                                }
                                wasSplit = true;
                            }
                            else
                            {
                                richTextBox1.Invoke(new Action(() =>
                                {
                                    richTextBox1.AppendText($"""
                                    POSTING: {message}
                                
                                    """);
                                }));

                                if (!wasSplit)
                                {
                                    if (sendAsThread)
                                    {
                                        await _discordClient.GetGuild(guildID).GetTextChannel(createdChannelId).SendMessageAsync(messageToSend).ConfigureAwait(false);
                                        var threadMessages = await _discordClient.GetGuild(guildID).GetTextChannel(createdChannelId).GetMessagesAsync(1).FlattenAsync();
                                        threadID = await _discordClient.GetGuild(guildID).GetTextChannel(createdChannelId).CreateThreadAsync("Slackord Thread", ThreadType.PublicThread, ThreadArchiveDuration.OneDay, threadMessages.First());
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
                                            richTextBox1.Invoke(new MethodInvoker(delegate ()
                                            {
                                                richTextBox1.AppendText("Caught a Slackdump thread reply exception where a JSON entry had thread_ts and wasn't actually a thread start or reply before it excepted. Sending as a normal message...");
                                            }));
                                            await _discordClient.GetGuild(guildID).GetTextChannel(createdChannelId).SendMessageAsync(messageToSend);
                                        }
                                    }
                                    else if (sendAsNormalMessage)
                                    {
                                        await _discordClient.GetGuild(guildID).GetTextChannel(createdChannelId).SendMessageAsync(messageToSend).ConfigureAwait(false);
                                    }
                                }
                                UpdateProgressBar();
                            }
                        }
                    }
                    await _discordClient.SetActivityAsync(new Game("for the Slackord command...", ActivityType.Listening));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            richTextBox1.Invoke(new Action(() =>
            {
                richTextBox1.AppendText("""
                 -----------------------------------------
                All messages sent to Discord successfully!

                """);
            }));
            await interaction.FollowupAsync("All messages sent to Discord successfully!", ephemeral: true);
            await _discordClient.SetActivityAsync(new Game("messages parse.", ActivityType.Watching));
            await Task.CompletedTask;
        }

        public async Task MainAsync()
        {
            richTextBox1.AppendText("Starting Slackord bot..." + "\n");
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
            EnableBotDisconnectionMenuItem();
            DisableTokenChangeWhileConnected();
            await _discordClient.LoginAsync(TokenType.Bot, _discordToken.Trim());
            await _discordClient.StartAsync();
            await _discordClient.SetActivityAsync(new Game("for messages to begin parsing.", ActivityType.Watching));
            _discordClient.Ready += ClientReady;
            _discordClient.SlashCommandExecuted += SlashCommandHandler;
            await Task.Delay(-1);
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.Data.Name.Equals("slackord"))
            {
                var guildID = _discordClient.Guilds.FirstOrDefault().Id;
                await PostMessagesToDiscord(guildID, command);
            }
        }

        private async Task ClientReady()
        {
            try
            {
                foreach (var guild in _discordClient.Guilds)
                {
                    var guildCommand = new SlashCommandBuilder();
                    guildCommand.WithName("slackord");
                    guildCommand.WithDescription("Posts all parsed Slack JSON messages to the text channel the command came from.");
                    try
                    {
                        await guild.CreateApplicationCommandAsync(guildCommand.Build());
                    }
                    catch (HttpException ex)
                    {
                        Console.WriteLine($"Error creating slash command in guild {guild.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error encountered while creating slash command: {ex.Message}");
            }
        }

        private Task DiscordClient_Log(LogMessage arg)
        {
            richTextBox1.Invoke(new Action(() => { richTextBox1.AppendText(arg.ToString() + "\n"); }));
            return Task.CompletedTask;
        }

        private static string DeDupeURLs(string input)
        {
            input = input.Replace("<", "").Replace(">", "");

            string[] parts = input.Split('|');

            if (parts.Length == 2)
            {
                if (Uri.TryCreate(parts[0], UriKind.Absolute, out Uri uri1) &&
                    Uri.TryCreate(parts[1], UriKind.Absolute, out Uri uri2))
                {
                    if (uri1.GetLeftPart(UriPartial.Path) == uri2.GetLeftPart(UriPartial.Path))
                    {
                        input = input.Replace(parts[1] + "|", "");
                    }
                }
            }

            string[] parts2 = input.Split('|').Distinct().ToArray();
            input = string.Join("|", parts2);

            return input;
        }

        private void UpdateProgressBar(int value = 1)
        {
            currentMessageCount += value;
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action<int>(UpdateProgressBar), value);
            }
            else
            {
                progressBar1.Value = currentMessageCount;
                progressBar1.Maximum = totalMessageCount;
                progressBar1.Style = ProgressBarStyle.Continuous;

                var textSize = 16;
                var textFont = new Font("Arial", textSize, FontStyle.Bold);
                var textBrush = Brushes.Black;
                var textX = (progressBar1.Width / 2) - (textSize * 2);
                var textY = (progressBar1.Height / 2) - (textSize / 2);

                progressBar1.CreateGraphics().DrawString(currentMessageCount.ToString() + " / " + totalMessageCount.ToString(),
                    textFont,
                    textBrush,
                    new PointF(textX, textY));
            }
        }

        private void CheckForUpdatesToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (_octoClient == null)
            {
                _octoClient = new GitHubClient(new ProductHeaderValue("Slackord2"));
                CheckForUpdates();
            }
            else if (_octoClient != null)
            {
                CheckForUpdates();
            }
            else
            {
                MessageBox.Show("Couldn't connect to get updates. Github must be down, try checking again later?",
                    "Couldn't Connect!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"""
                Slackord {CurrentVersion}.
                Created by Thomas Loupe.
                Github: https://github.com/thomasloupe
                Twitter: https://twitter.com/acid_rain
                Website: https://thomasloupe.com
                """, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static DateTime ConvertFromUnixTimestampToHumanReadableTime(double timestamp)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var returnDate = date.AddSeconds(timestamp);
            return returnDate;
        }

        private void EnterBotTokenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowEnterTokenDialog();
        }

        private void ConnectBotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = MainAsync();
        }

        private void ShowEnterTokenDialog()
        {
            _discordToken = Prompt.ShowDialog("Enter bot token.", "Enter Bot Token");
            Properties.Settings.Default.SlackordBotToken = _discordToken.Trim();
            if (_discordToken.Length > 10 && string.IsNullOrEmpty(_discordToken).Equals(false))
            {
                EnableBotConnectionMenuItem();
            }
            else
            {
                EnableBotDisconnectionMenuItem();
            }
        }

        private async void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await _discordClient.StopAsync();
            Application.Exit();
        }

        private void SaveSettingsEventHandler(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.FormHeight = Height;
            Properties.Settings.Default.FormWidth = Width;
            Properties.Settings.Default.FormLocation = Location;
            if (Properties.Settings.Default.FirstRun)
            {
                Properties.Settings.Default.FirstRun = false;
            }
            Properties.Settings.Default.SlackordBotToken = _discordToken.Trim();
            Properties.Settings.Default.Save();
        }

        private void DisconnectBotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _discordClient.StopAsync();
            EnableBotConnectionMenuItem();
            EnableTokenChangeWhileConnected();
        }

        private async void CheckForUpdates()
        {
            var releases = await _octoClient.Repository.Release.GetAll("thomasloupe", "Slackord-2.0").ConfigureAwait(false);
            var latest = releases[0];
            if (CurrentVersion == latest.TagName)
            {
                MessageBox.Show("You have the latest version, " + CurrentVersion + "!", CurrentVersion, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (CurrentVersion != latest.TagName)
            {
                var result = MessageBox.Show($"""
                A new version of Slackord is available!
                Current version: {CurrentVersion}
                Latest version: {latest.TagName}
                Would you like to visit the download page?
                """, "Update Available!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (result == DialogResult.Yes)
                {
                    Process.Start("https://github.com/thomasloupe/Slackord-2.0/releases/tag/" +
                                                     latest.TagName);
                }
            }
        }

        private void ToolStripButton1_Click(object sender, EventArgs e)
        {
            if (richTextBox1.SelectionLength == 0)
            {
                richTextBox1.SelectAll();
                richTextBox1.Copy();
            }
        }

        private void ToolStripButton2_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = "";
        }

        private void EnableBotConnectionMenuItem()
        {
            ConnectBotToolStripMenuItem.Enabled = true;
            DisconnectBotToolStripMenuItem.Enabled = false;
        }

        private void EnableBotDisconnectionMenuItem()
        {
            ConnectBotToolStripMenuItem.Enabled = false;
            DisconnectBotToolStripMenuItem.Enabled = true;
        }
        private void DisableBothBotConnectionButtons()
        {
            ConnectBotToolStripMenuItem.Enabled = false;
            DisconnectBotToolStripMenuItem.Enabled = false;
        }

        private void DisableTokenChangeWhileConnected()
        {
            EnterBotTokenToolStripMenuItem.Enabled = false;
        }
        private void EnableTokenChangeWhileConnected()
        {
            EnterBotTokenToolStripMenuItem.Enabled = true;
        }

        private void DonateToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            var url = "https://paypal.me/thomasloupe";
            var message = """
                Slackord will always be free!
                If you'd like to buy me a beer anyway, I won't tell you no!
                Would you like to open the donation page now?
                """;
            var result = MessageBox.Show(message, "Slackord is free, but beer is not!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (result == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
        }

        private void UpdateDebugWindowView()
        {
            richTextBox1.SelectionStart = richTextBox1.Text.Length - 1;
            richTextBox1.SelectionLength = 0;
            richTextBox1.ScrollToCaret();
        }

        private void Link_Clicked(object sender, LinkClickedEventArgs e)
        {
            var url = e.LinkText;
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }

        public static class Prompt
        {
            public static string ShowDialog(string text, string caption)
            {
                var prompt = new Form() { Width = 500, Height = 150, FormBorderStyle = FormBorderStyle.FixedDialog, Text = caption, StartPosition = FormStartPosition.CenterScreen };
                var textLabel = new Label() { Left = 50, Top = 20, Text = text };
                var textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
                var confirmation = new Button() { Text = "OK", Left = 225, Width = 50, Top = 75, DialogResult = DialogResult.OK };
                confirmation.Click += (sender, e) => { prompt.Close(); };
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(confirmation);
                prompt.Controls.Add(textLabel);
                prompt.AcceptButton = confirmation;
                return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
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
