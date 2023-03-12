// Slackord2 - Written by Thomas Loupe
// https://github.com/thomasloupe/Slackord2
// https://thomasloupe.com

using System.IO;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using Octo = Octokit;
using Microsoft.Extensions.DependencyInjection;
using MaterialSkin;
using MaterialSkin.Controls;
using System.Diagnostics;
using Application = System.Windows.Forms.Application;
using Label = System.Windows.Forms.Label;
using Discord.Net;
using Octokit;
using System.Text.RegularExpressions;
using Discord.Interactions;

namespace Slackord
{
    public partial class Slackord : MaterialForm
    {
        private const string CurrentVersion = "v2.4.4";
        public DiscordSocketClient _discordClient;
        private OpenFileDialog _ofd;
        private string _discordToken;
        private Octo.GitHubClient _octoClient;
        public bool _isFileParsed;
        private bool _isParsingNow;
        public bool _showDebugOutput = false;
        public IServiceProvider _services;
        public JArray parsed;
        private readonly List<string> Responses = new();
        private readonly List<string> ListOfFilesToParse = new();
        private readonly List<bool> isThreadMessages = new();
        private readonly List<bool> isThreadStart = new();

        public Slackord()
        {
            InitializeComponent();
            SetWindowSizeAndLocation();
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.Orange600, Primary.DeepOrange700, Primary.Amber500, Accent.Amber700, TextShade.BLACK);
            _isFileParsed = false;
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
                richTextBox1.Text += "Welcome to Slackord 2!" + "\n";
            }
            else if (string.IsNullOrEmpty(_discordToken) || string.IsNullOrEmpty(Properties.Settings.Default.SlackordBotToken))
            {
                richTextBox1.Text += """
                Slackord 2 tried to automatically load your last bot token but wasn't successful.
                The token is not long enough or the token value is empty. Please enter a new token.
                """;
            }
            else
            {
                richTextBox1.Text += "Slackord 2 found a previously entered bot token and automatically applied it! Bot connection is now enabled." + "\n";
                EnableBotConnectionMenuItem();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        [STAThread]
        private async void ParseJsonFiles()
        {
            _isParsingNow = true;

            richTextBox1.Text += """
            Begin parsing JSON data...
            -----------------------------------------

            """;

            foreach (var file in ListOfFilesToParse)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    parsed = JArray.Parse(json);
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
                                    Debug.WriteLine(ex.Message);
                                    continue;
                                }
                            }
                            debugResponse = fileLink;
                            if (!disableDebugOutputToolStripMenuItem.Checked)
                            {
                                richTextBox1.Text += debugResponse + "\n";
                            }
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
                            if (!disableDebugOutputToolStripMenuItem.Checked)
                            {
                                richTextBox1.Text += debugResponse + "\n";
                            }
                        }
                        if (pair.ContainsKey("user_profile") && pair.ContainsKey("text"))
                        {
                            var rawTimeDate = pair["ts"];
                            var oldDateTime = (double)rawTimeDate;
                            var convertDateTime = ConvertFromUnixTimestampToHumanReadableTime(oldDateTime).ToString("g");
                            var newDateTime = convertDateTime.ToString();
                            var slackUserName = pair["user_profile"]["display_name"].ToString();
                            var slackRealName = pair["user_profile"]["real_name"];

                            string slackMessage = pair["text"].ToString();

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
                                    if (!disableDebugOutputToolStripMenuItem.Checked)
                                    {
                                        richTextBox1.Text += $"""
                                        The following parse is over 2000 characters. Discord does not allow messages over 2000 characters.
                                        This message will be split into multiple posts. The message that will be split is: {debugResponse}
                                        """;
                                    }
                                }
                                else
                                {
                                    debugResponse = newDateTime + " - " + slackUserName + ": " + slackMessage + " " + "\n";
                                    Responses.Add(debugResponse);
                                }
                            }
                            if (!disableDebugOutputToolStripMenuItem.Checked)
                            {
                                richTextBox1.Text += debugResponse + "\n";
                            }
                        }
                    }
                    richTextBox1.Text += $"""
                    -----------------------------------------
                    Parsing of {file} completed successfully!
                    -----------------------------------------
                    
                    """;
                    _isFileParsed = true;
                    richTextBox1.ForeColor = System.Drawing.Color.DarkGreen;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                if (_discordClient != null)
                {
                    await _discordClient.SetActivityAsync(new Game("awaiting command to import messages...", ActivityType.Watching));
                }

            }
            _isParsingNow = false;
        }

        [SlashCommand("slackord", "Posts all parsed Slack JSON messages to the text channel the command came from.")]
        public async Task PostMessagesToDiscord(SocketChannel channel, ulong guildID, SocketInteraction interaction)
        {
            await interaction.DeferAsync();
            if (_isParsingNow)
            {
                MessageBox.Show("Slackord is currently parsing one or more JSON files. Please wait until parsing has finished until attempting to post messages.");
                return;
            }

            try
            {
                await _discordClient.SetActivityAsync(new Game("posting messages...", ActivityType.Watching));
                int messageCount = 0;

                if (_isFileParsed)
                {
                    richTextBox1.Invoke(new Action(() =>
                    {
                        richTextBox1.Text += """
                        Beginning transfer of Slack messages to Discord...
                        -----------------------------------------
                        """;
                    }));

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

                        if (message.Length >= 2000)
                        {
                            var responses = messageToSend.SplitInParts(1800);

                            richTextBox1.Invoke(new Action(() =>
                            {
                                richTextBox1.Text += "SPLITTING AND POSTING: " + messageToSend;
                            }));
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
                                        richTextBox1.Invoke(new MethodInvoker(delegate ()
                                        {
                                            richTextBox1.Text += "Caught a Slackdump thread reply exception where a JSON entry had thread_ts and wasn't actually a thread start or reply before it excepted. Sending as a normal message...";
                                        }));
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
                            richTextBox1.Invoke(new Action(() =>
                            {
                                richTextBox1.Text += $"""
                                POSTING: {message}
                                
                                """;
                            }));

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
                                        richTextBox1.Invoke(new MethodInvoker(delegate()
                                        {
                                            richTextBox1.Text += "Caught a Slackdump thread reply exception where a JSON entry had thread_ts and wasn't actually a thread start or reply before it excepted. Sending as a normal message...";
                                        }));
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
                    richTextBox1.Invoke(new Action(() =>
                    {
                        richTextBox1.Text += """
                        -----------------------------------------
                        All messages sent to Discord successfully!
                        """;
                    }));
                    // TODO: Fix Application did not respond in time error.
                    await interaction.FollowupAsync("All messages sent to Discord successfully!", ephemeral: true);
                    await _discordClient.SetActivityAsync(new Game("awaiting parsing of messages.", ActivityType.Watching));
                }
                else if (!_isFileParsed)
                {
                    await _discordClient.GetGuild(guildID).GetTextChannel(channel.Id).SendMessageAsync("Sorry, there's nothing to post because no JSON file was parsed prior to sending this command.").ConfigureAwait(false);
                    richTextBox1.Invoke(new Action(() =>
                    {
                        richTextBox1.Text += "Received a command to post messages to Discord, but no JSON file was parsed prior to receiving the command." + "\n";
                    }));
                }
                await _discordClient.SetActivityAsync(new Game("for the Slackord command...", ActivityType.Listening));
                Responses.Clear();
            }
            catch (Exception ex)
            {
                var exception = ex.GetType().ToString();
                if (exception.Equals("Discord.Net.HttpException"))
                {
                    switch (ex.Message)
                    {
                        case "The server responded with error 50006: Cannot send an empty message":
                            MessageBox.Show("The server responded with error 50006: Cannot send an empty message");
                            break;
                    }
                }
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

        public async Task MainAsync()
        {
            richTextBox1.Text += "Starting Slackord bot..." + "\n";
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
            await _discordClient.SetActivityAsync(new Game("and awaiting parsing of messages.", ActivityType.Watching));
            _discordClient.Ready += ClientReady;
            _discordClient.SlashCommandExecuted += SlashCommandHandler;
            await Task.Delay(-1);
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.Data.Name.Equals("slackord"))
            {
                var guildID = _discordClient.Guilds.FirstOrDefault().Id;
                var channel = _discordClient.GetChannel((ulong)command.ChannelId);
                await PostMessagesToDiscord(channel, guildID, command);
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

        private Task DiscordClient_Log(LogMessage arg)
        {
            richTextBox1.Invoke(new Action(() => { richTextBox1.Text += arg.ToString() + "\n"; }));
            return Task.CompletedTask;
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
            var result = MessageBox.Show("""
                Slackord will always be free!
                If you'd like to buy me a beer anyway, I won't tell you no!
                Would you like to open the donation page now?
                """, "Slackord is free, but beer is not!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (result == DialogResult.Yes)
            {
                Process.Start("https://paypal.me/thomasloupe");
            }
        }

        private void RichTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
        }
        private void Link_Clicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListOfFilesToParse.Clear();
            _ofd = new OpenFileDialog { Filter = "JSON File|*.json", Title = "Import a JSON file for parsing" };
            if (_ofd.ShowDialog() == DialogResult.OK)
                ListOfFilesToParse.Add(_ofd.FileName);
            ParseJsonFiles();
        }

        private void ImportJSONFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListOfFilesToParse.Clear();
            using var fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                var files = Directory.EnumerateFiles(fbd.SelectedPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(s => s.EndsWith(".JSON") || s.EndsWith(".json"));

                foreach (var file in files)
                {
                    ListOfFilesToParse.Add(file);
                }
                MessageBox.Show("Files found: " + files.Count(), "Message");
                ParseJsonFiles();
            }
        }

        private void DisableDebugOutputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (disableDebugOutputToolStripMenuItem.Checked)
            {
                disableDebugOutputToolStripMenuItem.Checked = false;
            }
            else
            {
                disableDebugOutputToolStripMenuItem.Checked = true;
            }
        }

        private void CreateChannelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_discordClient == null || _discordClient.ConnectionState == ConnectionState.Disconnected || _discordClient.ConnectionState == ConnectionState.Disconnecting)
            {
                MessageBox.Show("You must be connected to Discord to create channels!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _ofd = new OpenFileDialog { Filter = "JSON File|*.json", Title = "Import a JSON file for parsing" };
            if (_ofd.ShowDialog() == DialogResult.OK)
            {
                var json = File.ReadAllText(_ofd.FileName);
                parsed = JArray.Parse(json);
            }

            var result = MessageBox.Show($"""
                    It is assumed that you have not created any channels with the names of the channels in the JSON file yet. If you have, you will more than likely see duplicate channels.
                    Now is a good time to remove any channels you do not want to create duplicates of. When ready, press "Yes" to continue.
                    """, "Warning!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (result == DialogResult.Yes)
            {
                List<string> ChannelsToCreate = new();

                foreach (JObject pair in parsed.Cast<JObject>())
                {
                    if (pair.ContainsKey("name"))
                    {
                        ChannelsToCreate.Add(pair["name"].ToString());
                    }
                }
                CreateChannelsAsync(ChannelsToCreate).ConfigureAwait(false);
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
                    MessageBox.Show(ex.Message);
                    return;
                }
            }
            MessageBox.Show($"""
                            Channel import completed!
                            The following channels were created:

                            {string.Join(Environment.NewLine, _channelsToCreate)}
                            """, "Channel Import Complete!", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
    }
    static class StringExtensions
    {
        public static IEnumerable<string> SplitInParts(this string s, Int32 partLength)
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
