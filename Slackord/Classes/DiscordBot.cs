using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using MenuApp;

namespace Slackord.Classes
{
    class DiscordBot
    {
        public static DiscordBot Instance { get; private set; } = new DiscordBot();
        public DiscordSocketClient DiscordClient { get; set; }
        public IServiceProvider _services;
        public Dictionary<ulong, RestTextChannel> CreatedChannels { get; set; } = new Dictionary<ulong, RestTextChannel>();
        private DiscordBot() { }

        public async Task StartClientAsync()
        {
            await DiscordClient.StartAsync();
        }

        public async Task StopClientAsync()
        {
            await DiscordClient.StopAsync();
        }

        public ConnectionState GetClientConnectionState()
        {
            return DiscordClient.ConnectionState;
        }

        private Task DiscordClient_Log(LogMessage arg)
        {
            ApplicationWindow.WriteToDebugWindow(arg.ToString() + "\n");
            return Task.CompletedTask;
        }

        private async Task OnClientDisconnect()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await ApplicationWindow.ToggleBotTokenEnable(true, new Microsoft.Maui.Graphics.Color(255, 69, 0));
                await ApplicationWindow.ChangeBotConnectionButton("Disconnected", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                await Task.CompletedTask;
            });
            await Task.CompletedTask;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.Data.Name.Equals("slackord"))
            {
                var guildId = command.GuildId;
                await PostMessagesToDiscord((ulong)guildId, command);
            }
        }

        public async Task MainAsync(string discordToken)
        {
            if (DiscordClient is not null)
            {
                throw new InvalidOperationException("DiscordClient is already initialized.");
            }
            ApplicationWindow.WriteToDebugWindow("Starting Slackord Bot..." + "\n");

            // Configure the DiscordSocketClient
            DiscordSocketConfig _config = new()
            {
                GatewayIntents = GatewayIntents.DirectMessages | GatewayIntents.GuildMessages | GatewayIntents.Guilds
            };

            // Initialize the DiscordClient
            DiscordClient = new DiscordSocketClient(_config);

            // Set up dependency injection
            _services = new ServiceCollection()
                .AddSingleton(DiscordClient)
                .BuildServiceProvider();

            // Assign event handlers
            DiscordClient.Log += DiscordClient_Log;
            DiscordClient.Ready += ClientReady;
            DiscordClient.LoggedOut += OnClientDisconnect;
            DiscordClient.SlashCommandExecuted += SlashCommandHandler;

            // Login and start the client
            await DiscordClient.LoginAsync(TokenType.Bot, discordToken.Trim());
            await DiscordClient.StartAsync();

            // Set the client's activity
            await DiscordClient.SetActivityAsync(new Game("for the Slackord command!", ActivityType.Watching));
        }

        private async Task ClientReady()
        {
            try
            {
                await ApplicationWindow.ChangeBotConnectionButton("Connected", new Microsoft.Maui.Graphics.Color(0, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await ApplicationWindow.ToggleBotTokenEnable(false, new Microsoft.Maui.Graphics.Color(128, 128, 128));
                MainPage.BotConnectionButtonInstance.BackgroundColor = new Microsoft.Maui.Graphics.Color(0, 255, 0);

                foreach (var guild in DiscordClient.Guilds)
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
                        Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"\nError creating slash command in guild {guild.Name}: {ex.Message}\n"); });
                    }
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"\nError encountered while creating slash command: {ex.Message}\n"); });
            }
        }

        [SlashCommand("slackord", "Posts all parsed Slack JSON messages to the text channel the command came from.")]
        public async Task PostMessagesToDiscord(ulong guildID, SocketInteraction interaction)
        {
            try
            {
                await interaction.DeferAsync();
                await ReconstructSlackChannelsOnDiscord(guildID);
                var task = PostMessagesToDiscord(guildID: guildID);
                await task;
                await interaction.FollowupAsync("All messages sent to Discord successfully!");
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Error: {ex.Message}\n"); });
                await interaction.FollowupAsync($"""
                An exception was encountered while sending messages! The exception was:
                {ex.Message}
                """);
            }
        }

        public async Task ReconstructSlackChannelsOnDiscord(ulong guildID)
        {
            try
            {
                SocketGuild guild = DiscordClient.GetGuild(guildID);
                string categoryName = "Slackord Import";
                var slackordCategory = await guild.CreateCategoryChannelAsync(categoryName);
                ulong slackordCategoryId = slackordCategory.Id;

                foreach (var channel in ImportJson.Channels)
                {
                    try
                    {
                        var channelName = channel.Name.ToLower();
                        var createdRestChannel = await guild.CreateTextChannelAsync(channelName, properties =>
                        {
                            properties.CategoryId = slackordCategoryId;
                        });

                        ulong createdChannelId = createdRestChannel.Id;
                        channel.DiscordChannelId = createdChannelId;

                        CreatedChannels[createdChannelId] = createdRestChannel;
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Error: {ex.Message}\n"); });
                    }
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Error: {ex.Message}\n"); });
            }
        }

        public async Task PostMessagesToDiscord(ulong guildID)
        {
            Application.Current.Dispatcher.Dispatch(() =>
            {
                ApplicationWindow.WriteToDebugWindow($"PostMessagesToDiscord called with guildID: {guildID}\n");
            });

            // Assuming ImportJson.Channels holds the list of Channel objects
            foreach (var channel in ImportJson.Channels)
            {
                try
                {
                    if (CreatedChannels.TryGetValue(channel.DiscordChannelId, out var discordChannel))
                    {
                        // Iterate through the ReconstructedMessagesList and post each message to the Discord channel
                        foreach (var message in channel.ReconstructedMessagesList)
                        {
                            try
                            {
                                await discordChannel.SendMessageAsync(message.Content);
                            }
                            catch (Exception ex)
                            {
                                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Error: {ex.Message}\n"); });
                            }
                        }
                    }
                    else
                    {
                        // Handle the case where the Discord channel is not found or the ID is incorrect
                        Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Discord channel not found for channel: {channel.Name}\n"); });
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Error: {ex.Message}\n"); });
                }
            }
        }
    }
}