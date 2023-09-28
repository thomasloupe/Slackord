using Discord;
using Discord.Interactions;
using Discord.Net;
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
            ApplicationWindow.WriteToDebugWindow("Starting Slackord Bot..." + "\n");
            DiscordSocketConfig _config = new();
            {
                _config.GatewayIntents = GatewayIntents.DirectMessages | GatewayIntents.GuildMessages | GatewayIntents.Guilds;
            }
            DiscordClient = new(_config);
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
                        ApplicationWindow.WriteToDebugWindow($"\nError creating slash command in guild {guild.Name}: {ex.Message}\n");
                    }
                }
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"\nError encountered while creating slash command: {ex.Message}\n");
            }
        }

        private Task DiscordClient_Log(LogMessage arg)
        {
            ApplicationWindow.WriteToDebugWindow(arg.ToString() + "\n");
            return Task.CompletedTask;
        }

        public async Task DisconectClient()
        {
            await ApplicationWindow.ChangeBotConnectionButton("Disconnecting", new Microsoft.Maui.Graphics.Color(255, 204, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
            await DiscordClient.StopAsync();
            await ApplicationWindow.ToggleBotTokenEnable(true, new Microsoft.Maui.Graphics.Color(255, 69, 0));
            await ApplicationWindow.ChangeBotConnectionButton("Disconnected", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
            await Task.CompletedTask;
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
        [SlashCommand("slackord", "Posts all parsed Slack JSON messages to the text channel the command came from.")]
        public async Task PostMessagesToDiscord(ulong guildID, SocketInteraction interaction)
        {
            await interaction.DeferAsync();

            SocketGuild guild = DiscordClient.GetGuild(guildID);
            string categoryName = "Slackord Import";
            var slackordCategory = await guild.CreateCategoryChannelAsync(categoryName);
            ulong slackordCategoryId = slackordCategory.Id;

            var userManager = new UserManager(usersFilePath);  // Assume usersFilePath is the path to your users file
            var threads = Parser.Convert(ImportJson);  // Get the threads using the Parser.Convert method

            foreach (var thread in threads)
            {

                var channel = thread.Messages.First();  // Assume each message has a Channel property indicating its channel
                var channelToCreate = channel.Name;
                var createdChannel = await guild.CreateTextChannelAsync(channelToCreate, properties =>
                {
                    properties.CategoryId = slackordCategoryId;
                });

                ulong createdChannelId = createdChannel.Id;
                ApplicationWindow.WriteToDebugWindow($"Created {channelToCreate} on Discord with ID: {createdChannelId}.\n");

                try
                {
                    await DiscordClient.SetActivityAsync(new Game("messages...", ActivityType.Streaming));

                    foreach (var message in thread.Messages)
                    {
                        var textContent = Reconstruct.ConvertTextContent(message.Text);
                        var discordEmbedBuilder = Reconstruct.ConvertAttachments(message.Attachments);
                        var discordReactions = Reconstruct.ConvertReactions(message.Reactions);

                        // Replace Slack user IDs with user names
                        textContent = userManager.GetUserName(message.UserId) + ": " + textContent;

                        // If textContent is not empty, send it as a separate message.
                        if (!string.IsNullOrWhiteSpace(textContent))
                        {
                            var sentMessage = await createdChannel.SendMessageAsync(textContent);
                            ApplicationWindow.WriteToDebugWindow($"Sent message: {textContent} to channel {createdChannel.Name}\n");

                            // Add reactions to the sent message
                            foreach (var reaction in discordReactions)
                            {
                                await sentMessage.AddReactionAsync(new Emoji(reaction));
                            }
                        }

                        // If there are attachments or files, send them as an embed in a separate message.
                        if (discordEmbedBuilder.Fields.Count > 0)
                        {
                            var sentEmbedMessage = await createdChannel.SendMessageAsync(embed: discordEmbedBuilder.Build());
                            ApplicationWindow.WriteToDebugWindow($"Sent embed message to channel {createdChannel.Name}\n");

                            // Optionally, add reactions to the sent embed message too, if needed.
                            // foreach (var reaction in discordReactions)
                            // {
                            //     await sentEmbedMessage.AddReactionAsync(new Emoji(reaction));
                            // }
                        }
                    }

                    await interaction.FollowupAsync("All messages sent to Discord successfully!", ephemeral: true);
                    await DiscordClient.SetActivityAsync(new Game("to some cool music!", ActivityType.Listening));
                }
                catch (Exception ex)
                {
                    ApplicationWindow.WriteToDebugWindow(ex.Message);
                }
            }
        }

    }
}