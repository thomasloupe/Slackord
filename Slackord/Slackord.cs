using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using octo = Octokit;
using Microsoft.Extensions.DependencyInjection;

namespace Slackord
{
    public partial class Slackord : Form
    {
        private const string CurrentVersion = "v2.1.1";
        private const string CommandTrigger = "!slackord";
        private DiscordSocketClient _discordClient;
        private OpenFileDialog _ofd;
        private string _discordToken;
        private octo.GitHubClient _octoClient;
        private bool _isFileParsed;
        private IServiceProvider _services;
        private bool _failOnMessagesOverCharLimit;
        private int _skippedMessages;

        public Slackord()
        {
            InitializeComponent();
            SetWindowSizeAndLocation();
            _isFileParsed = false;
            CheckForExistingBotToken();
        }

        private void SetWindowSizeAndLocation()
        {
            this.Location = Properties.Settings.Default.FormLocation;
            this.Height = Properties.Settings.Default.FormHeight;
            this.Width = Properties.Settings.Default.FormWidth;
            this.FormClosing += SaveSettingsEventHandler;
            this.StartPosition = FormStartPosition.Manual;
        }

        private void CheckForExistingBotToken()
        {
            DisableBothBotConnectionButtons();
            _discordToken = Properties.Settings.Default.SlackordBotToken;
            if (Properties.Settings.Default.FirstRun)
            {
                richTextBox1.Text += "Welcome to Slackord 2!" + "\n";
            }
            else if (String.IsNullOrEmpty(_discordToken) || String.IsNullOrEmpty(Properties.Settings.Default.SlackordBotToken))
            {
                richTextBox1.Text += "Slackord 2 tried to automatically load your last bot token but wasn't successful." + "\n" 
                    + "The token is not long enough or the token value is empty. Please enter a new token." + "\n";
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

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectFileAndConvertJson();
        }

        [STAThread]
        private void SelectFileAndConvertJson()
        {
            _ofd = new OpenFileDialog { Filter = "JSON File|*.json", Title = "Import a JSON file for parsing" };
            if (_ofd.ShowDialog() == DialogResult.OK)
            {
                var json = File.ReadAllText(_ofd.FileName);
                var parsed = JArray.Parse(json);
                var parseFailed = false;
                richTextBox1.Text += "Begin parsing JSON data..." + "\n";
                richTextBox1.Text += "-----------------------------------------" + "\n";
                foreach (JObject pair in parsed)
                {
                    var debugResponse = "";
                    if (pair.ContainsKey("files"))
                    {
                        try
                        {
                            debugResponse = ("Parsed: " + pair["files"][0]["thumb_1024"] + "\n".ToString());
                        }
                        catch (NullReferenceException)
                        {
                            debugResponse = ("Parsed: " + pair["files"][0]["url_private"] + "\n".ToString());
                        }
                        richTextBox1.Text += debugResponse + "\n";
                    }
                    if (pair.ContainsKey("user_profile") && pair.ContainsKey("text")) 
                    {
                        var rawTimeDate = pair["ts"];
                        var oldDateTime = (double)rawTimeDate;
                        var convertDateTime = ConvertFromUnixTimestampToHumanReadableTime(oldDateTime);
                        var newDateTime = convertDateTime.ToString();
                        var slackUserName = pair["user_profile"]["display_name"].ToString();
                        var slackRealName = pair["user_profile"]["real_name"];
                        var slackMessage = pair["text"];
                        _skippedMessages = 0;
                        if (String.IsNullOrEmpty(slackUserName))
                        {
                            debugResponse = "Parsed: " + newDateTime + " - " + slackRealName + ": " + slackMessage;
                        }
                        else
                        {
                            debugResponse = "Parsed: " + newDateTime + " - " + slackUserName + ": " + slackMessage;
                            if (debugResponse.Length >= 2000)
                            {
                                if (_failOnMessagesOverCharLimit)
                                {
                                    parseFailed = true;
                                    richTextBox1.Text += "\n" + "PARSING FAILED!" + "\n" +
                                        "A message which contained more than 2000 characters was discovered. Discord does not allow messages over 2000 characters." +
                                        "Please edit your JSON file or posting to Discord will fail." + "\n" +
                                        "For your information, the message was: " + "\n" + "\n";
                                    break;
                                }
                                else
                                {
                                    richTextBox1.Text += "\n" + "PARSING FAILED ON MESSAGE! CONTINUING..." + "\n" +
                                        "A message which contained more than 2000 characters was discovered. Discord does not allow messages over 2000 characters." +
                                        "Please edit your JSON file or posting to Discord will fail." + "\n" +
                                        "For your information, the message was: " + "\n" + "\n";
                                    _skippedMessages++;
                                }
                            }
                            else
                            {
                                debugResponse = "Parsed: " + newDateTime + " - " + slackUserName + ": " + slackMessage;
                            }
                        }
                        richTextBox1.Text += debugResponse + "\n";
                    }
                }
                richTextBox1.Text += "-----------------------------------------" + "\n";
                if (parseFailed)
                {
                    richTextBox1.Text += "FAILED TO PARSE ONE OR MORE MESSAGES! PLEASE SEE THE LOG" + "\n";
                    _isFileParsed = false;
                    richTextBox1.ForeColor = Color.Red;
                }
                else
                {
                    if (_skippedMessages > 0)
                    {
                        richTextBox1.Text += "Parsing completed, but there were " + _skippedMessages + " skipped messages." + "\n";
                    }
                    else
                    {
                        richTextBox1.Text += "Parsing completed successfully!" + "\n";
                        _isFileParsed = true;
                        richTextBox1.ForeColor = Color.DarkGreen;
                    }

                }
                _skippedMessages = 0;
            }
        }

        private void CheckForUpdatesToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (_octoClient == null)
            {
                _octoClient = new octo.GitHubClient(new octo.ProductHeaderValue("Slackord2"));
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
            MessageBox.Show("Slackord " + CurrentVersion + ".\n" +
                "Created by Thomas Loupe." + "\n" +
                "Github: https://github.com/thomasloupe" + "\n" +
                "Twitter: https://twitter.com/acid_rain" + "\n" +
                "Website: https://thomasloupe.com", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static DateTime ConvertFromUnixTimestampToHumanReadableTime(double timestamp)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
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
            _services = new ServiceCollection()
            .AddSingleton(_discordClient)
            .BuildServiceProvider();
            _discordClient.Log += DiscordClient_Log;
            EnableBotDisconnectionMenuItem();
            DisableTokenChangeWhileConnected();
            await _discordClient.LoginAsync(TokenType.Bot, _discordToken).ConfigureAwait(false);
            await _discordClient.StartAsync().ConfigureAwait(false);
            _discordClient.MessageReceived += MessageReceived;
            await Task.Delay(-1).ConfigureAwait(false);
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

        private async Task MessageReceived(SocketMessage message)
        {
            if (_isFileParsed && message.Content.Equals(CommandTrigger, StringComparison.OrdinalIgnoreCase))
            {
                var json = File.ReadAllText(_ofd.FileName);
                var parsed = JArray.Parse(json);
                richTextBox1.Invoke(new Action(() =>
                {
                    richTextBox1.Text += "Beginning transfer of Slack messages to Discord..." + "\n" +
                    "-----------------------------------------" + "\n";
                }));
                foreach (JObject pair in parsed)
                {
                    var slackordResponse = "";
                    if (pair.ContainsKey("files"))
                    {
                        try
                        {
                            slackordResponse = pair["text"] + "\n" + pair["files"][0]["thumb_1024"] + "\n".ToString();
                        }
                        catch (NullReferenceException)
                        {
                            slackordResponse = pair["text"] + "\n" + pair["files"][0]["url_private"] + "\n".ToString();
                        }

                        richTextBox1.Invoke(new Action(() =>
                        {
                            richTextBox1.Text += "POSTING: " + slackordResponse;
                        }));
                        await message.Channel.SendMessageAsync(slackordResponse).ConfigureAwait(false);
                    }
                    if (!pair.ContainsKey("user_profile") && !pair.ContainsKey("text"))
                    {
                        richTextBox1.Invoke(new Action(() =>
                        {
                            richTextBox1.ForeColor = Color.Red;
                            richTextBox1.Text += "A MESSAGE THAT COULDN'T BE SENT WAS SKIPPED HERE." + "\n";
                            richTextBox1.ForeColor = Color.Green;
                        }));
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
                        var slackMessage = pair["text"];
                        if (String.IsNullOrEmpty(slackUserName))
                        {
                            slackordResponse = newDateTime + " - " + slackRealName + ": " + slackMessage + "\n";
                        }
                        else
                        {
                            slackordResponse = newDateTime + " - " + slackUserName + ": " + slackMessage + "\n";
                        }
                        if (slackordResponse.Length >= 2000)
                        {
                            richTextBox1.Invoke(new Action(() =>
                            {
                                richTextBox1.ForeColor = Color.DarkOrange;
                                richTextBox1.Text += "SKIPPING: " + slackordResponse;
                                richTextBox1.ForeColor = Color.Green;
                            }));
                        }
                        else
                        {
                            richTextBox1.Invoke(new Action(() =>
                            {
                                richTextBox1.Text += "POSTING: " + slackordResponse;
                            }));
                            await message.Channel.SendMessageAsync(slackordResponse).ConfigureAwait(false);
                        }
                    }
                }
                richTextBox1.Invoke(new Action(() =>
                {
                    richTextBox1.Text += "-----------------------------------------" + "\n" +
                    "All messages sent to Discord successfully!" + "\n";
                }));
            }
            else if (!_isFileParsed && message.Content.Equals(CommandTrigger, StringComparison.OrdinalIgnoreCase))
            {
                await message.Channel.SendMessageAsync("Sorry, there's nothing to post because no JSON file was parsed prior to sending this command.").ConfigureAwait(false);
                richTextBox1.Invoke(new Action(() =>
                {
                    richTextBox1.Text += "Received a command to post messages to Discord, but no JSON file was parsed prior to receiving the command." + "\n";
                }));
            }
        }

        private void ShowEnterTokenDialog()
        {
            _discordToken = Prompt.ShowDialog("Enter bot token.", "Enter Bot Token");
            Properties.Settings.Default.SlackordBotToken = _discordToken;
            if (_discordToken.Length >10 && String.IsNullOrEmpty(_discordToken).Equals(false))
            {
                EnableBotConnectionMenuItem();
            }
            else
            {
                EnableBotDisconnectionMenuItem();
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _discordClient.StopAsync();
            Application.Exit();
        }

        private void SaveSettingsEventHandler(object sender, FormClosingEventArgs e) 
        {
            Properties.Settings.Default.FormHeight = this.Height;
            Properties.Settings.Default.FormWidth = this.Width;
            Properties.Settings.Default.FormLocation = this.Location;
            if (Properties.Settings.Default.FirstRun) 
            {
                Properties.Settings.Default.FirstRun = false;
            }
            Properties.Settings.Default.SlackordBotToken = _discordToken;
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
                var result = MessageBox.Show("A new version of Slackord is available!\n"
                    + "Current version: " + CurrentVersion + "\n"
                    + "Latest version: " + latest.TagName + "\n"
                    + "Would you like to visit the download page?", "Update Available!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (result == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("https://github.com/thomasloupe/Slackord-2.0/releases/tag/" +
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
            var result = MessageBox.Show("Slackord will always be free!\n"
                + "If you'd like to buy me a beer anyway, I won't tell you no!\n"
                + "Would you like to open the donation page now?", "Slackord is free, but beer is not!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (result == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start("https://paypal.me/thomasloupe");
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
               richTextBox1.SelectionStart = richTextBox1.Text.Length;
               richTextBox1.ScrollToCaret();
        }
        private void Link_Clicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }

        private void failOnCharacterLimitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (failOnCharacterLimitToolStripMenuItem.Checked)
            {
                _failOnMessagesOverCharLimit = false;
            }
            else
            {
                _failOnMessagesOverCharLimit = true;
            }
        }
    }

    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            var prompt = new Form(){Width = 500,Height = 150,FormBorderStyle = FormBorderStyle.FixedDialog,Text = caption,StartPosition = FormStartPosition.CenterScreen};
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
