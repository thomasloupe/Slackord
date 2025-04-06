﻿using Discord;
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
        private CancellationTokenSource _cancellationTokenSource;

        public async Task StartClientAsync(string discordToken)
        {
            bool isConnected = false;
            int maxRetryAttempts = 3;
            int delayMilliseconds = 5000;

            for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
            {
                try
                {
                    if (DiscordClient.LoginState != LoginState.LoggedIn)
                    {
                        await DiscordClient.LoginAsync(TokenType.Bot, discordToken.Trim());
                    }
                    await DiscordClient.StartAsync();
                    isConnected = true;
                    break;
                }
                catch (Exception ex)
                {
                    ApplicationWindow.WriteToDebugWindow($"Attempt {attempt}: Failed to start DiscordClient - {ex.Message}\n");
                    if (attempt < maxRetryAttempts)
                    {
                        await Task.Delay(delayMilliseconds);
                        delayMilliseconds = Math.Min(delayMilliseconds * 2, 30000); // Double the delay, up to 30 seconds
                    }
                }
            }

            if (!isConnected)
            {
                ApplicationWindow.WriteToDebugWindow($"Failed to connect after {maxRetryAttempts} attempts.\n");
                await ApplicationWindow.ChangeBotConnectionButton("Disconnected", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
            }
        }

        public async Task LogoutClientAsync()
        {
            try
            {
                await DiscordClient.LogoutAsync();
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"Error while stopping the Discord client: {ex.Message}");
            }
        }

        public ConnectionState GetClientConnectionState()
        {
            return DiscordClient?.ConnectionState ?? ConnectionState.Disconnected;
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
                // Updated to match new signature (removed color parameter)
                await ApplicationWindow.ToggleBotTokenEnable(true);
                await ApplicationWindow.ChangeBotConnectionButton("Disconnected", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                await Task.CompletedTask;
            });
            await Task.CompletedTask;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            ApplicationWindow.ResetProgressBar();
            ApplicationWindow.ShowProgressBar();

            if (command.Data.Name.Equals("slackord"))
            {
                ulong? guildId = command.GuildId;
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

            // Configure the DiscordSocketClient.
            DiscordSocketConfig _config = new()
            {
                GatewayIntents = GatewayIntents.DirectMessages | GatewayIntents.GuildMessages | GatewayIntents.Guilds,
                UseInteractionSnowflakeDate = false
            };

            // Initialize the DiscordClient.
            DiscordClient = new DiscordSocketClient(_config);

            // Set up dependency injection.
            _services = new ServiceCollection()
                .AddSingleton(DiscordClient)
                .BuildServiceProvider();

            // Assign event handlers
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

        private async Task ClientReady()
        {
            try
            {
                await ApplicationWindow.ChangeBotConnectionButton("Connected", new Microsoft.Maui.Graphics.Color(0, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                // Updated to match new signature (removed color parameter)
                await ApplicationWindow.ToggleBotTokenEnable(false);
                MenuApp.MainPage.BotConnectionButtonInstance.BackgroundColor = new Microsoft.Maui.Graphics.Color(0, 255, 0);

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
                _cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = _cancellationTokenSource.Token;

                SocketGuild guild = DiscordClient.GetGuild(guildID);
                string baseCategoryName = "Slackord Import";

                SocketCategoryChannel currentCategory = null;
                ulong currentCategoryId = 0;

                // Attempt to find an existing category named "Slackord Import".
                currentCategory = guild.CategoryChannels.FirstOrDefault(c => c.Name.StartsWith(baseCategoryName));

                if (currentCategory == null)
                {
                    var createdCategory = await guild.CreateCategoryChannelAsync(baseCategoryName);
                    currentCategoryId = createdCategory.Id;
                }
                else
                {
                    currentCategoryId = currentCategory.Id;
                }

                int channelCountInCurrentCategory = guild.Channels.Count(c => c is SocketTextChannel && c.Id == currentCategoryId);

                foreach (Channel channel in ImportJson.Channels)
                {
                    if (channelCountInCurrentCategory >= 50)
                    {
                        // Create a new category if the existing one or previously created is full.
                        var newCategory = await guild.CreateCategoryChannelAsync($"{baseCategoryName} {currentCategoryId + 1}");
                        currentCategoryId = newCategory.Id;
                        channelCountInCurrentCategory = 0; // Reset the count for the new category.
                    }

                    string channelName = channel.Name.ToLower();
                    // Handle potential channel name conflicts.
                    if (guild.TextChannels.Any(c => c.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase) && c.CategoryId == currentCategoryId))
                    {
                        int suffix = 1;
                        string newName;
                        do
                        {
                            newName = $"{channelName}-{suffix++}";
                        } while (guild.TextChannels.Any(c => c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase) && c.CategoryId == currentCategoryId));

                        channelName = newName;
                    }

                    var createdChannel = await guild.CreateTextChannelAsync(channelName, properties =>
                    {
                        properties.CategoryId = currentCategoryId;
                        properties.Topic = channel.Description;
                    });

                    // Set the DiscordChannelId and populate the CreatedChannels dictionary.
                    ulong createdChannelId = createdChannel.Id;
                    channel.DiscordChannelId = createdChannelId;
                    CreatedChannels[createdChannelId] = createdChannel;

                    channelCountInCurrentCategory++;
                }
            }
            catch (OperationCanceledException)
            {
                _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Channel creation was canceled.\n"); });
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"ReconstructSlackChannelsOnDiscord: {ex.Message}\n");
            }
            finally
            {
                _cancellationTokenSource.Dispose();
            }
        }

        private static void RecordSuccessfulMessage(Channel channel, ReconstructedMessage message)
        {
            try
            {
                // Only record if resume is enabled
                if (Preferences.Default.Get("EnableResumeImport", true))
                {
                    string importType = ImportJson.Channels.Count > 1 ? "Full" : "Channel";

                    // Save the resume state
                    Preferences.Default.Set("LastImportType", importType);
                    Preferences.Default.Set("LastImportChannel", channel.Name);
                    Preferences.Default.Set("LastSuccessfulMessageTimestamp", message.OriginalTimestamp);
                    Preferences.Default.Set("HasPartialImport", true);

                    // We can't set ImportJson.RootFolderPath directly, but we can save the path
                    if (!string.IsNullOrEmpty(ImportJson.RootFolderPath))
                    {
                        Preferences.Default.Set("LastImportFolderPath", ImportJson.RootFolderPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error recording successful message: {ex.Message}");
            }
        }

        private static void ClearResumeState()
        {
            // Clear the resume state
            Preferences.Default.Set("LastImportType", string.Empty);
            Preferences.Default.Set("LastImportChannel", string.Empty);
            Preferences.Default.Set("LastSuccessfulMessageTimestamp", string.Empty);
            Preferences.Default.Set("HasPartialImport", false);
        }

        [SlashCommand("slackord", "Posts all parsed Slack JSON messages to the text channel the command came from.")]
        public async Task PostMessagesToDiscord(ulong guildID, SocketInteraction interaction)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

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
                _ = await interaction.FollowupAsync($"An exception was encountered while sending messages! The exception was:\n{ex.Message}");
                return;
            }

            // Determine the file size limit based on the server's boost level
            SocketGuild guild = DiscordClient.GetGuild(guildID);
            long fileSizeLimit = guild.PremiumTier switch
            {
                PremiumTier.Tier1 => 8 * 1024 * 1024, // 8 MB
                PremiumTier.Tier2 => 50 * 1024 * 1024, // 50 MB
                PremiumTier.Tier3 => 100 * 1024 * 1024, // 100 MB
                _ => 8 * 1024 * 1024 // Default to 8 MB for non-boosted servers
            };

            Dictionary<string, RestThreadChannel> threadStartsDict = new();
            int messagesPosted = 0;
            bool errorOccurred = false;
            ApplicationWindow.ResetProgressBar();

            foreach (Channel channel in ImportJson.Channels)
            {
                if (CreatedChannels.TryGetValue(channel.DiscordChannelId, out RestTextChannel discordChannel))
                {
                    var webhook = await discordChannel.CreateWebhookAsync("Slackord Temp Webhook");
                    string webhookUrl = $"https://discord.com/api/webhooks/{webhook.Id}/{webhook.Token}";
                    using var webhookClient = new DiscordWebhookClient(webhookUrl);

                    foreach (ReconstructedMessage message in channel.ReconstructedMessagesList)
                    {
                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            ulong? threadIdForReply = null;
                            bool shouldArchiveThreadBack = false;

                            if (message.ThreadType == ThreadType.Parent)
                            {
                                string threadName = string.IsNullOrEmpty(message.Message) ? "Replies" : message.Message.Length <= 20 ? message.Message : message.Message[..20];
                                await webhookClient.SendMessageAsync(message.Content, false, null, message.User, message.Avatar);
                                IEnumerable<RestMessage> threadMessages = await discordChannel.GetMessagesAsync(1).FlattenAsync();
                                RestThreadChannel threadID = await discordChannel.CreateThreadAsync(threadName, Discord.ThreadType.PublicThread, ThreadArchiveDuration.OneHour, threadMessages.First());
                                threadStartsDict[message.ParentThreadTs] = threadID;

                                // Record successful message
                                RecordSuccessfulMessage(channel, message);
                            }
                            else if (message.ThreadType == ThreadType.Reply)
                            {
                                if (threadStartsDict.TryGetValue(message.ParentThreadTs, out RestThreadChannel threadID))
                                {
                                    if (threadID.IsArchived)
                                    {
                                        await threadID.ModifyAsync(properties => properties.Archived = false);
                                        shouldArchiveThreadBack = true;
                                    }

                                    threadIdForReply = threadID.Id;
                                }
                                else
                                {
                                    _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Parent message not found for thread reply: {message.Content}\n"); });
                                }

                                await webhookClient.SendMessageAsync(message.Content, false, null, message.User, message.Avatar, threadId: threadIdForReply);

                                // Record successful message
                                RecordSuccessfulMessage(channel, message);

                                if (shouldArchiveThreadBack)
                                {
                                    try
                                    {
                                        await threadID.ModifyAsync(properties => properties.Archived = true);
                                        _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Archived thread: {threadID.Name}\n"); });
                                    }
                                    catch (Exception archiveEx)
                                    {
                                        _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Error archiving thread: {archiveEx.Message}\n"); });
                                    }
                                }
                            }
                            else
                            {
                                await webhookClient.SendMessageAsync(message.Content, false, null, message.User, message.Avatar);

                                // Record successful message
                                RecordSuccessfulMessage(channel, message);
                            }

                            // Pin message.
                            if (message.IsPinned && message.ThreadType == ThreadType.None)
                            {
                                IEnumerable<IMessage> recentMessages = await discordChannel.GetMessagesAsync(1).Flatten().ToListAsync();
                                IMessage recentMessage = recentMessages.FirstOrDefault();
                                if (recentMessage is IUserMessage userMessage)
                                {
                                    await userMessage.PinAsync();
                                }
                            }

                            // Handle file uploads.
                            List<string> localFilePaths = message.FileURLs.ToList(); // Convert to list for easy indexing

                            for (int i = 0; i < localFilePaths.Count; i++)
                            {
                                var localFilePath = localFilePaths[i];
                                if (File.Exists(localFilePath))
                                {
                                    FileInfo fileInfo = new(localFilePath);
                                    long fileSizeInBytes = fileInfo.Length;

                                    if (fileSizeInBytes <= fileSizeLimit)
                                    {
                                        using FileStream fs = new(localFilePath, FileMode.Open, FileAccess.Read);
                                        try
                                        {
                                            await discordChannel.SendFileAsync(fs, Path.GetFileName(localFilePath));
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Log($"Failed to upload file {localFilePath}: {ex.Message}");
                                        }
                                    }
                                    else
                                    {
                                        // If the file is too large, check if a fallback URL exists and send the permalink instead.
                                        if (message.FallbackFileURLs.Count > i)
                                        {
                                            string downloadLink = message.FallbackFileURLs[i];
                                            await discordChannel.SendMessageAsync($"File was too large to upload. You can download it [here]({downloadLink}).");
                                        }
                                        else
                                        {
                                            await discordChannel.SendMessageAsync("File was too large to upload, and a download link is not available.");
                                        }
                                    }
                                }
                                else
                                {
                                    Logger.Log($"File not found: {localFilePath}");
                                    await discordChannel.SendMessageAsync("Attachment:");
                                }
                            }

                            messagesPosted++;
                            // Update the progress bar.
                            ApplicationWindow.UpdateProgressBar(messagesPosted, totalMessagesToPost, "messages");
                        }
                        catch (OperationCanceledException)
                        {
                            errorOccurred = true;
                            _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Posting messages cancelled.\n"); });
                            break;
                        }
                        catch (Exception ex)
                        {
                            errorOccurred = true;
                            _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"PostMessagesToDiscord(): {ex.Message}\n"); });

                            // Offer to retry or resume later
                            bool shouldRetry = await MainPage.Current.DisplayAlert(
                                "Error Posting Message",
                                $"An error occurred while posting a message: {ex.Message}\n\nWould you like to retry posting this message?",
                                "Retry", "Stop");

                            if (shouldRetry)
                            {
                                // Retry the current message by decrementing the loop counter
                                // This will process the same message again
                                continue;
                            }
                            else
                            {
                                // Save the current state for resume
                                RecordSuccessfulMessage(channel, message);
                                await MainPage.Current.DisplayAlert(
                                    "Import Paused",
                                    "The import process has been paused. You can resume it later by restarting Slackord.",
                                    "OK");
                                break;
                            }
                        }
                    }

                    if (errorOccurred)
                    {
                        break;
                    }

                    await webhook.DeleteAsync();
                }
                else
                {
                    _ = Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Discord channel not found for channel: {channel.Name}\n"); });
                }

                if (errorOccurred)
                {
                    break;
                }
            }

            if (!errorOccurred)
            {
                // All messages were posted successfully, clear resume state
                ClearResumeState();
                _ = await interaction.FollowupAsync("All messages sent to Discord successfully!");
            }
            else
            {
                _ = await interaction.FollowupAsync("Message sending was interrupted. You can resume the process later.");
            }
        }
    }
}
