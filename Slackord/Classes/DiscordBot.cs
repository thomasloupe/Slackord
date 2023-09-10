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
        public DiscordSocketClient DiscordClient { get; set; }
        public IServiceProvider _services;

        public async Task MainAsync(string discordToken)
        {
            if (DiscordClient is not null)
            {
                throw new InvalidOperationException("DiscordClient is already initialized.");
            }
            MainPage.WriteToDebugWindow("Starting Slackord Bot..." + "\n");
            MainPage.PushDebugText();
            DiscordSocketConfig _config = new()
            {
                GatewayIntents = GatewayIntents.DirectMessages | GatewayIntents.GuildMessages | GatewayIntents.Guilds
            };
            DiscordClient = new DiscordSocketClient(_config);
            _services = new ServiceCollection()
                .AddSingleton(DiscordClient)
                .BuildServiceProvider();
            DiscordClient.Log += DiscordClient_Log;
            await DiscordClient.LoginAsync(TokenType.Bot, discordToken.Trim());
            await DiscordClient.StartAsync();
            await DiscordClient.SetActivityAsync(new Game("for the Slackord command!", ActivityType.Watching));
            DiscordClient.Ready += ClientReady;
            DiscordClient.LoggedOut += OnClientDisconnect;
            DiscordClient.SlashCommandExecuted += SlashCommandHandler;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.Data.Name.Equals("slackord"))
            {
                var guildId = command.GuildId;
                await PostMessagesToDiscord((ulong)guildId, command);
            }
        }

        private async Task ClientReady()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await MainPage.ChangeBotConnectionButton("Connected", new Microsoft.Maui.Graphics.Color(0, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await MainPage.ToggleBotTokenEnable(false, new Microsoft.Maui.Graphics.Color(128, 128, 128));
                MainPage.BotConnectionButtonInstance.BackgroundColor = new Microsoft.Maui.Graphics.Color(0, 255, 0);
            });

            foreach (var guild in DiscordClient.Guilds)
            {
                var guildCommand = new SlashCommandBuilder()
                    .WithName("slackord")
                    .WithDescription("Posts all parsed Slack JSON messages to the text channel the command came from.")
                    .Build();

                try
                {
                    await guild.CreateApplicationCommandAsync(guildCommand);
                }
                catch (HttpException ex)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MainPage.WriteToDebugWindow($"\nError creating slash command in guild {guild.Name}: {ex.Message}\n");
                        MainPage.PushDebugText();
                    });
                }
            }
        }

        private Task DiscordClient_Log(LogMessage arg)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage.WriteToDebugWindow(arg.ToString() + "\n");
                MainPage.PushDebugText();
            });

            return Task.CompletedTask;
        }

        public async Task DisconectClient()
        {
            await MainPage.ChangeBotConnectionButton("Disconnecting", new Microsoft.Maui.Graphics.Color(255, 204, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
            await DiscordClient.StopAsync();
            await MainPage.ToggleBotTokenEnable(true, new Microsoft.Maui.Graphics.Color(255, 69, 0));
            await MainPage.ChangeBotConnectionButton("Disconnected", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
        }

        private async Task OnClientDisconnect()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await MainPage.ToggleBotTokenEnable(true, new Microsoft.Maui.Graphics.Color(255, 69, 0));
                await MainPage.ChangeBotConnectionButton("Disconnected", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
            });
        }

        [SlashCommand("slackord", "Posts all parsed Slack JSON messages to the text channel the command came from.")]
        public async Task PostMessagesToDiscord(ulong guildID, SocketInteraction interaction)
        {
            var totalMessageCount = Parser.TotalMessageCount;
            float progress = 0;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await MainPage.UpdateMessageSendProgress(progress, totalMessageCount);
            });

            await interaction.DeferAsync();

            SocketGuild guild = DiscordClient.GetGuild(guildID);
            string categoryName = "Slackord Import";
            var slackordCategory = await guild.CreateCategoryChannelAsync(categoryName);
            ulong slackordCategoryId = slackordCategory.Id;

            var channels = ImportJson.Channels;

            foreach (var channelName in channels.Keys)
            {
                var createdChannel = await guild.CreateTextChannelAsync(channelName, properties =>
                {
                    properties.CategoryId = slackordCategoryId;
                });

                ulong createdChannelId = createdChannel.Id;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage.WriteToDebugWindow($"Created {channelName} on Discord with ID: {createdChannelId}.\n");
                });

                await DiscordClient.SetActivityAsync(new Game("messages...", ActivityType.Streaming));

                if (channels.TryGetValue(channelName, out var messages))
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MainPage.WriteToDebugWindow($"Beginning transfer of Slack messages to Discord for {channelName}...\n-----------------------------------------");
                    });

                    RestThreadChannel threadID = null;

                    foreach (Message message in messages)
                    {
                        string messageToSend = $"{message.Timestamp} - {message.Text}";

                        if (messageToSend.Length >= 2000)
                        {
                            // Break down long messages into parts that Discord can handle (2000 char limit per message)
                            int maxMessageLength = 2000; // Discord's max message length
                            int numberOfParts = (int)Math.Ceiling((double)messageToSend.Length / maxMessageLength);
                            RestThreadChannel currentThreadID = threadID;

                            for (int i = 0; i < numberOfParts; i++)
                            {
                                int startIndex = i * maxMessageLength;
                                int length = Math.Min(maxMessageLength, messageToSend.Length - startIndex);

                                string partOfMessage = messageToSend.Substring(startIndex, length);

                                // Send each part as a separate message
                                if (message.IsThreadStart && i == 0)
                                {
                                    await createdChannel.SendMessageAsync(partOfMessage).ConfigureAwait(false);
                                    var threadMessages = await createdChannel.GetMessagesAsync(1).FlattenAsync();
                                    currentThreadID = await createdChannel.CreateThreadAsync("New Thread", ThreadType.PublicThread, ThreadArchiveDuration.OneDay, threadMessages.First());
                                }
                                else if (message.IsThreadMessage && currentThreadID != null)
                                {
                                    await currentThreadID.SendMessageAsync(partOfMessage);
                                }
                                else
                                {
                                    await createdChannel.SendMessageAsync(partOfMessage);
                                }
                            }
                        }
                        else
                        {
                            // The logic for messages that don't need to be split
                            if (message.IsThreadStart)
                            {
                                await createdChannel.SendMessageAsync(messageToSend).ConfigureAwait(false);
                                var threadMessages = await createdChannel.GetMessagesAsync(1).FlattenAsync();
                                string threadName = $"Thread-{message.Text.Substring(0, Math.Min(10, message.Text.Length))}-{message.Timestamp}";
                                threadID = await createdChannel.CreateThreadAsync(threadName, ThreadType.PublicThread, ThreadArchiveDuration.OneDay, threadMessages.First());
                            }
                            else if (message.IsThreadMessage && threadID != null)
                            {
                                await threadID.SendMessageAsync(messageToSend);
                            }
                            else
                            {
                                await createdChannel.SendMessageAsync(messageToSend);
                            }
                        }

                        progress += 1;
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await MainPage.UpdateMessageSendProgress(progress, totalMessageCount);
                        });
                    }
                }
                await DiscordClient.SetActivityAsync(new Game("for the Slackord command...", ActivityType.Listening));
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage.WriteToDebugWindow($"-----------------------------------------\nAll messages sent to Discord successfully!\n");
            });

            await interaction.FollowupAsync("All messages sent to Discord successfully!", ephemeral: true);
            await DiscordClient.SetActivityAsync(new Game("to some cool music!", ActivityType.Listening));
        }
    }
}
