using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using MenuApp;

namespace Slackord.Classes
{
    internal class DiscordBot
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
            try
            {
                ApplicationWindow.ResetProgressBar();
                ApplicationWindow.ShowProgressBar();

                if (command.Data.Name.Equals("slackord"))
                {
                    ulong? guildId = command.GuildId;
                    await PostMessagesToDiscord((ulong)guildId, command);
                }
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"Exception in SlashCommandHandler() : {ex.Message}\n");
            }
        }

        public async Task MainAsync(string discordToken)
        {
            try
            {
                if (DiscordClient is not null)
                {
                    throw new InvalidOperationException("DiscordClient is already initialized.");
                }
                ApplicationWindow.WriteToDebugWindow("Starting Slackord Bot...\n");

                // Configure the DiscordSocketClient.
                DiscordSocketConfig _config = new()
                {
                    GatewayIntents = GatewayIntents.DirectMessages | GatewayIntents.GuildMessages | GatewayIntents.Guilds
                };

                // Initialize the DiscordClient.
                //DiscordClient = new DiscordSocketClient(_config);
                DiscordClient = new DiscordSocketClient();

                // Set up dependency injection.
                _services = new ServiceCollection()
                    .AddSingleton(DiscordClient)
                    .BuildServiceProvider();

                // Assign event handlers.
                DiscordClient.Log += DiscordClient_Log;
                DiscordClient.Ready += ClientReady;
                DiscordClient.LoggedOut += OnClientDisconnect;
                DiscordClient.SlashCommandExecuted += SlashCommandHandler;

                // Login and start the client.
                await DiscordClient.LoginAsync(TokenType.Bot, discordToken.Trim());
                await DiscordClient.StartAsync();

                // Set the client's activity.
                await DiscordClient.SetActivityAsync(new Game("for the Slackord command!", ActivityType.Watching));
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"Exception in MainAsync() : {ex.Message}\n");
            }
            
        }

        private async Task ClientReady()
        {
            try
            {
                await ApplicationWindow.ChangeBotConnectionButton("Connected", new Microsoft.Maui.Graphics.Color(0, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await ApplicationWindow.ToggleBotTokenEnable(false, new Microsoft.Maui.Graphics.Color(128, 128, 128));
                MainPage.BotConnectionButtonInstance.BackgroundColor = new Microsoft.Maui.Graphics.Color(0, 255, 0);

                foreach (SocketGuild guild in DiscordClient.Guilds)
                {
                    try
                    {
                        var commandBuilder = new SlashCommandBuilder()
                                                .WithName("slackord")
                                                .WithDescription("Posts all parsed Slack JSON messages to the text channel the command came from.");

                        _ = await guild.CreateApplicationCommandAsync(commandBuilder.Build());
                    }
                    catch (HttpException ex)
                    {
                        _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"\nError creating slash command in guild {guild.Name}: {ex.Message}\n"); });
                    }
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"\nError encountered while creating slash command: {ex.Message}\n"); });
            }
        }

        public async Task ReconstructSlackChannelsOnDiscord(ulong guildID)
        {
            try
            {
                SocketGuild guild = DiscordClient.GetGuild(guildID);
                string baseCategoryName = "Slackord Import";
                int categoryCount = 1;
                string currentCategoryName = baseCategoryName + (categoryCount > 1 ? $" {categoryCount}" : "");

                RestCategoryChannel currentCategory = await guild.CreateCategoryChannelAsync(currentCategoryName);
                ulong currentCategoryId = currentCategory.Id;

                int channelCountInCurrentCategory = 0;

                foreach (Channel channel in ImportJson.Channels)
                {
                    try
                    {
                        if (channelCountInCurrentCategory >= 50)  // If we've reached the 50 channel limit in this category, create a new category.
                        {
                            categoryCount++;
                            currentCategoryName = baseCategoryName + $" {categoryCount}";
                            currentCategory = await guild.CreateCategoryChannelAsync(currentCategoryName);
                            currentCategoryId = currentCategory.Id;
                            channelCountInCurrentCategory = 0;
                        }

                        string channelName = channel.Name.ToLower();
                        RestTextChannel createdRestChannel = await guild.CreateTextChannelAsync(channelName, properties =>
                        {
                            properties.CategoryId = currentCategoryId;
                            properties.Topic = channel.Description;
                        });

                        ulong createdChannelId = createdRestChannel.Id;
                        channel.DiscordChannelId = createdChannelId;
                        CreatedChannels[createdChannelId] = createdRestChannel;

                        channelCountInCurrentCategory++;
                    }
                    catch (Exception ex)
                    {
                        _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Error: {ex.Message}\n"); });
                    }
                }
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Error: {ex.Message}\n"); });
            }
        }

        [SlashCommand("slackord", "Posts all parsed Slack JSON messages to the text channel the command came from.")]
        public async Task PostMessagesToDiscord(ulong guildID, SocketInteraction interaction)
        {
            _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"PostMessagesToDiscord called with guildID: {guildID}\n"); });
            int totalMessagesToPost = ImportJson.Channels.Sum(channel => channel.ReconstructedMessagesList.Count);
            _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Total messages to send to Discord: {totalMessagesToPost}\n"); });

            try
            {
                await interaction.DeferAsync();
                await ReconstructSlackChannelsOnDiscord(guildID);
            }
            catch (Exception ex)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Error: {ex.Message}\n"); });
                _ = await interaction.FollowupAsync($"""
An exception was encountered while sending messages! The exception was:
{ex.Message}
""");
            }

            Dictionary<string, RestThreadChannel> threadStartsDict = new();
            int messagesPosted = 0;
            ApplicationWindow.ResetProgressBar();

            foreach (Channel channel in ImportJson.Channels)
            {
                if (CreatedChannels.TryGetValue(channel.DiscordChannelId, out RestTextChannel discordChannel))
                {
                    // Create a webhook for the channel once
                    var webhook = await discordChannel.CreateWebhookAsync("Slackord Temp Webhook");
                    string webhookUrl = $"https://discord.com/api/webhooks/{webhook.Id}/{webhook.Token}";
                    using var webhookClient = new DiscordWebhookClient(webhookUrl);

                    foreach (ReconstructedMessage message in channel.ReconstructedMessagesList)
                    {
                        try
                        {
                            ulong? threadIdForReply = null;

                            if (message.ThreadType == ThreadType.Parent)
                            {
                                string threadName = message.Message.Length <= 20 ? message.Message : message.Message[..20];
                                await webhookClient.SendMessageAsync(message.Content, false, null, message.User, message.Avatar);

                                IEnumerable<RestMessage> threadMessages = await discordChannel.GetMessagesAsync(1).FlattenAsync();
                                RestThreadChannel threadID = await discordChannel.CreateThreadAsync(threadName, Discord.ThreadType.PublicThread, ThreadArchiveDuration.OneDay, threadMessages.First());

                                threadStartsDict[message.ParentThreadTs] = threadID;
                            }
                            else if (message.ThreadType == ThreadType.Reply)
                            {
                                if (threadStartsDict.TryGetValue(message.ParentThreadTs, out RestThreadChannel threadID))
                                {
                                    threadIdForReply = threadID.Id;
                                }
                                else
                                {
                                    _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Parent message not found for thread reply: {message.Content}\n"); });
                                }

                                await webhookClient.SendMessageAsync(message.Content, false, null, message.User, message.Avatar, threadId: threadIdForReply);
                            }
                            else
                            {
                                await webhookClient.SendMessageAsync(message.Content, false, null, message.User, message.Avatar);
                            }

                            // Handle pinning the message.
                            if (message.IsPinned && message.ThreadType == ThreadType.None)
                            {
                                // Retrieve the most recent message from the channel.
                                IEnumerable<IMessage> recentMessages = await discordChannel.GetMessagesAsync(1).Flatten().ToListAsync();
                                IMessage recentMessage = recentMessages.FirstOrDefault();

                                // Pin the message.
                                if (recentMessage is IUserMessage userMessage)
                                {
                                    await userMessage.PinAsync();
                                }
                            }

                            messagesPosted++;
                            ApplicationWindow.UpdateProgressBar(messagesPosted, totalMessagesToPost, "messages");

                            await Task.Delay(1000);  // Delay to prevent rate limiting.
                        }
                        catch (Exception ex)
                        {
                            _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Error: {ex.Message}\n"); });
                        }
                    }

                    await webhook.DeleteAsync();  // Delete the webhook after all messages for the channel are sent.
                }
                else
                {
                    _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Discord channel not found for channel: {channel.Name}\n"); });
                }
            }

            _ = await interaction.FollowupAsync("All messages sent to Discord successfully!");
        }
    }
}
