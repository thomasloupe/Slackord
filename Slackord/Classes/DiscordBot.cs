using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using MenuApp;

namespace Slackord
{
    class DiscordBot
    {
        public DiscordSocketClient _discordClient;
        public IServiceProvider _services;

        public async Task MainAsync(string discordToken)
        {
            Editor debugWindow = new();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage.WriteToDebugWindow("Starting Slackord Bot..." + "\n");
            });
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
            await _discordClient.LoginAsync(TokenType.Bot, discordToken.Trim());
            await _discordClient.StartAsync();
            await _discordClient.SetActivityAsync(new Game("for the Slackord command!", ActivityType.Watching));
            _discordClient.Ready += ClientReady;
            _discordClient.LoggedOut += OnClientDisconnect;
            _discordClient.SlashCommandExecuted += SlashCommandHandler;
            await Task.Delay(-1);
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.Data.Name.Equals("slackord"))
            {
                var guildID = _discordClient.Guilds.FirstOrDefault().Id;
                await MainPage.Current.Dispatcher.DispatchAsync(async () =>
                {
                    await PostMessagesToDiscord(guildID, command);
                });
            }
        }

        private async Task ClientReady()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(async() =>
                {
                    await MainPage.ChangeBotConnectionButton("Connected", new Microsoft.Maui.Graphics.Color(0, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                    await MainPage.ToggleBotTokenEnable(false, new Microsoft.Maui.Graphics.Color(128, 128, 128));
                    MainPage.BotConnectionButtonInstance.BackgroundColor = new Microsoft.Maui.Graphics.Color(0, 255, 0);
                });

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
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            MainPage.WriteToDebugWindow($"\nError creating slash command in guild {guild.Name}: {ex.Message}\n");
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    MainPage.WriteToDebugWindow($"\nError encountered while creating slash command: {ex.Message}\n");
                });
            }
        }

        private Task DiscordClient_Log(LogMessage arg)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage.WriteToDebugWindow(arg.ToString() + "\n");
            });

            return Task.CompletedTask;
        }

        public async Task DisconectClient()
        {
            await MainPage.ChangeBotConnectionButton("Disconnecting", new Microsoft.Maui.Graphics.Color(255, 204, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
            await _discordClient.StopAsync();
            await MainPage.ToggleBotTokenEnable(true, new Microsoft.Maui.Graphics.Color(255, 69, 0));
            await MainPage.ChangeBotConnectionButton("Disconnected", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
            await Task.CompletedTask;
        }

        private async Task OnClientDisconnect()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await MainPage.ToggleBotTokenEnable(true, new Microsoft.Maui.Graphics.Color(255, 69, 0));
                await MainPage.ChangeBotConnectionButton("Disconnected", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                await Task.CompletedTask;
            });
            await Task.CompletedTask;
        }

        [SlashCommand("slackord", "Posts all parsed Slack JSON messages to the text channel the command came from.")]
        public async Task PostMessagesToDiscord(ulong guildID, SocketInteraction interaction)
        {
            var totalMessageCount = Parser.TotalMessageCount;
            float progress = 0;

            MainThread.BeginInvokeOnMainThread(async() =>
            {
                await MainPage.CommitProgress(progress, totalMessageCount);
            });

            await interaction.DeferAsync();

            SocketGuild guild = _discordClient.GetGuild(guildID);
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

                try
                {
                    await _discordClient.SetActivityAsync(new Game("messages...", ActivityType.Streaming));
                    int messageCount = 0;

                    if (channels.TryGetValue(channelName, out var messages))
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            MainPage.WriteToDebugWindow($"Beginning transfer of Slack messages to Discord for {channelName}..." + "\n" +
                                "-----------------------------------------");
                        });

                        RestThreadChannel threadID = null;

                        foreach (string message in messages)
                        {
                            bool sendAsThread = false;
                            bool sendAsThreadReply = false;
                            bool sendAsNormalMessage = false;

                            string messageToSend = message;
                            bool wasSplit = false;

                            var _isThreadStart = Parser.isThreadStart;
                            var _isThreadMessages = Parser.isThreadMessages;

                            if (_isThreadStart[messageCount] == true)
                            {
                                sendAsThread = true;
                            }
                            else if (_isThreadStart[messageCount] == false && _isThreadMessages[messageCount] == true)
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
                                var responses = Helpers.SplitInParts(messageToSend, 1800);

                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    MainPage.WriteToDebugWindow("SPLITTING AND POSTING: " + messageToSend);
                                });

                                foreach (var response in responses)
                                {
                                    messageToSend = response + " " + "\n";

                                    if (sendAsThread)
                                    {
                                        if (_discordClient.GetChannel(createdChannelId) is SocketTextChannel textChannel)
                                        {
                                            await textChannel.SendMessageAsync(messageToSend).ConfigureAwait(false);
                                            var latestMessages = await textChannel.GetMessagesAsync(1).FlattenAsync();
                                            threadID = threadID = await createdChannel.CreateThreadAsync("Slackord Thread", ThreadType.PublicThread, ThreadArchiveDuration.OneDay, latestMessages.First());
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
                                            MainThread.BeginInvokeOnMainThread(() =>
                                            {
                                                MainPage.WriteToDebugWindow("Caught a Slackdump thread reply exception where a JSON entry had thread_ts and wasn't actually a thread start or reply before it excepted. Sending as a normal message...");
                                            });
                                            await _discordClient.GetGuild(guildID).GetTextChannel(createdChannelId).SendMessageAsync(messageToSend).ConfigureAwait(false);
                                        }
                                    }
                                    else if (sendAsNormalMessage)
                                    {
                                        await _discordClient.GetGuild(guildID).GetTextChannel(createdChannelId).SendMessageAsync(messageToSend).ConfigureAwait(false);
                                    }

                                    progress += 1;
                                    MainThread.BeginInvokeOnMainThread(async () =>
                                    {
                                        await MainPage.CommitProgress(progress, totalMessageCount);
                                    });
                                }
                                wasSplit = true;
                            }
                            else
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    MainPage.WriteToDebugWindow($"POSTING: {message}\n");
                                });

                                if (!wasSplit)
                                {
                                    if (sendAsThread)
                                    {
                                        await createdChannel.SendMessageAsync(messageToSend).ConfigureAwait(false);
                                        var threadMessages = await createdChannel.GetMessagesAsync(1).FlattenAsync();
                                        threadID = await createdChannel.CreateThreadAsync("New Thread", ThreadType.PublicThread, ThreadArchiveDuration.OneDay, threadMessages.First());
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
                                            MainThread.BeginInvokeOnMainThread(() =>
                                            {
                                                MainPage.WriteToDebugWindow("Caught a Slackdump thread reply exception where a JSON entry had thread_ts and wasn't actually a thread start or reply before it excepted. Sending as a normal message...");
                                            });
                                            await createdChannel.SendMessageAsync(messageToSend);
                                        }
                                    }
                                    else if (sendAsNormalMessage)
                                    {
                                        await createdChannel.SendMessageAsync(messageToSend);
                                    }
                                }

                                progress += 1;
                                MainThread.BeginInvokeOnMainThread(async() =>
                                {
                                    await MainPage.CommitProgress(progress, totalMessageCount);
                                });
                            }
                        }
                    }
                    await _discordClient.SetActivityAsync(new Game("for the Slackord command...", ActivityType.Listening));
                }
                catch (Exception ex)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MainPage.WriteToDebugWindow($"\n{ex.Message}\n");
                    });
                    Page page = new();
                    await page.DisplayAlert("Error", ex.Message, "OK");
                }
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MainPage.WriteToDebugWindow($"-----------------------------------------\nAll messages sent to Discord successfully!\n");
            });

            await interaction.FollowupAsync("All messages sent to Discord successfully!", ephemeral: true);
            await _discordClient.SetActivityAsync(new Game("to some cool music!", ActivityType.Listening));
            await Task.CompletedTask;
        }
    }
}
