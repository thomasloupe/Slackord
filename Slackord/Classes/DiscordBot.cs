using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.Webhook;
using Discord.WebSocket;
using MenuApp;

namespace Slackord.Classes
{
    /// <summary>
    /// Manages Discord bot functionality including connection, message posting, and slash commands
    /// </summary>
    internal class DiscordBot
    {
        /// <summary>
        /// Gets the singleton instance of DiscordBot
        /// </summary>
        public static DiscordBot Instance { get; private set; } = new DiscordBot();

        /// <summary>
        /// Gets or sets the Discord socket client instance
        /// </summary>
        public DiscordSocketClient DiscordClient { get; set; }

        /// <summary>
        /// Gets or sets the service provider for dependency injection
        /// </summary>
        public IServiceProvider _services;

        /// <summary>
        /// Gets or sets the dictionary of created Discord channels indexed by channel ID
        /// </summary>
        public Dictionary<ulong, ITextChannel> CreatedChannels { get; set; } = [];

        /// <summary>
        /// Cancellation token source for Discord operations
        /// </summary>
        private CancellationTokenSource _discordOperationsCancellationTokenSource;

        /// <summary>
        /// Current log level for Discord bot logging
        /// </summary>
        private LogSeverity _currentLogLevel = LogSeverity.Info;

        /// <summary>
        /// Private constructor for singleton pattern
        /// </summary>
        private DiscordBot() { }

        /// <summary>
        /// Starts the Discord client with retry logic for connection failures
        /// </summary>
        /// <param name="discordToken">The Discord bot token for authentication</param>
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
                        delayMilliseconds = Math.Min(delayMilliseconds * 2, 30000);
                    }
                }
            }

            if (!isConnected)
            {
                ApplicationWindow.WriteToDebugWindow($"Failed to connect after {maxRetryAttempts} attempts.\n");
                await ApplicationWindow.ChangeBotConnectionButton("Disconnected", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
            }
        }

        /// <summary>
        /// Logs out the Discord client gracefully
        /// </summary>
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

        /// <summary>
        /// Cancels ongoing Discord operations
        /// </summary>
        public void CancelDiscordOperations()
        {
            _discordOperationsCancellationTokenSource?.Cancel();
            ApplicationWindow.WriteToDebugWindow("🛑 Discord operations cancellation requested - will stop after current message.\n");
        }

        /// <summary>
        /// Gets the current connection state of the Discord client
        /// </summary>
        /// <returns>The current connection state</returns>
        public ConnectionState GetClientConnectionState()
        {
            return DiscordClient?.ConnectionState ?? ConnectionState.Disconnected;
        }

        /// <summary>
        /// Updates the current log level for Discord bot logging
        /// </summary>
        /// <param name="logLevel">The new log level to apply</param>
        public void UpdateLogLevel(LogSeverity logLevel)
        {
            _currentLogLevel = logLevel;
            ApplicationWindow.WriteToDebugWindow($"🔧 Discord log level updated to: {GetLogLevelName(logLevel)}\n");
        }

        /// <summary>
        /// Gets a user-friendly name for the log level
        /// </summary>
        /// <param name="severity">The LogSeverity enum value</param>
        /// <returns>A human-readable log level name</returns>
        private static string GetLogLevelName(LogSeverity severity)
        {
            return severity switch
            {
                LogSeverity.Critical => "Critical",
                LogSeverity.Error => "Error",
                LogSeverity.Warning => "Warning",
                LogSeverity.Info => "Info",
                LogSeverity.Debug => "Debug",
                LogSeverity.Verbose => "Verbose",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets the log level setting from application preferences
        /// </summary>
        /// <returns>The LogSeverity value from preferences</returns>
        private static LogSeverity GetLogLevelFromPreferences()
        {
            int logLevel = Preferences.Default.Get("DiscordLogLevel", 3);
            return logLevel switch
            {
                0 => LogSeverity.Critical,
                1 => LogSeverity.Error,
                2 => LogSeverity.Warning,
                3 => LogSeverity.Info,
                4 => LogSeverity.Debug,
                5 => LogSeverity.Verbose,
                _ => LogSeverity.Info
            };
        }

        /// <summary>
        /// Handles Discord client log messages and filters based on current log level
        /// </summary>
        /// <param name="arg">The log message from Discord.NET</param>
        /// <returns>A completed task</returns>
        private Task DiscordClient_Log(LogMessage arg)
        {
            if (arg.Severity <= _currentLogLevel)
            {
                string logPrefix = GetLogLevelName(arg.Severity).ToUpper();
                string formattedMessage = $"[{logPrefix}] {arg}";
                ApplicationWindow.WriteToDebugWindow(formattedMessage + "\n");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles Discord client disconnection events
        /// </summary>
        /// <returns>A completed task</returns>
        private async Task OnClientDisconnect()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await ApplicationWindow.ToggleBotTokenEnable(true);
                await ApplicationWindow.ChangeBotConnectionButton("Disconnected", new Microsoft.Maui.Graphics.Color(255, 0, 0), new Microsoft.Maui.Graphics.Color(255, 255, 255));
                await Task.CompletedTask;
            });
            await Task.CompletedTask;
        }

        /// <summary>
        /// Handles incoming slash commands from Discord
        /// </summary>
        /// <param name="command">The slash command that was executed</param>
        /// <returns>A completed task</returns>
        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            ApplicationWindow.ResetProgressBar();
            ApplicationWindow.ShowProgressBar();

            if (command.Data.Name.Equals("slackord"))
            {
                ulong? guildId = command.GuildId;
                await PostMessagesToDiscord((ulong)guildId, command);
            }
            else if (command.Data.Name.Equals("resume"))
            {
                await HandleResumeCommandAsync(command);
            }
        }

        /// <summary>
        /// Initializes and starts the Discord bot with proper configuration
        /// </summary>
        /// <param name="discordToken">The Discord bot token for authentication</param>
        public async Task MainAsync(string discordToken)
        {
            if (DiscordClient is not null)
            {
                throw new InvalidOperationException("DiscordClient is already initialized.");
            }

            _currentLogLevel = GetLogLevelFromPreferences();
            ApplicationWindow.WriteToDebugWindow($"🔧 Discord log level set to: {GetLogLevelName(_currentLogLevel)}\n");
            ApplicationWindow.WriteToDebugWindow("Starting Slackord Bot..." + "\n");

            DiscordSocketConfig _config = new()
            {
                GatewayIntents = GatewayIntents.DirectMessages | GatewayIntents.GuildMessages | GatewayIntents.Guilds,
                UseInteractionSnowflakeDate = false,
                LogLevel = _currentLogLevel
            };

            DiscordClient = new DiscordSocketClient(_config);

            _services = new ServiceCollection()
                .AddSingleton(DiscordClient)
                .BuildServiceProvider();

            DiscordClient.Log += DiscordClient_Log;
            DiscordClient.Ready += ClientReady;
            DiscordClient.LoggedOut += OnClientDisconnect;
            DiscordClient.SlashCommandExecuted += SlashCommandHandler;

            await DiscordClient.LoginAsync(TokenType.Bot, discordToken.Trim());
            await DiscordClient.StartAsync();

            await DiscordClient.SetActivityAsync(new Game("for the Slackord command!", ActivityType.Watching));
        }

        /// <summary>
        /// Handles the Discord client ready event and sets up slash commands
        /// </summary>
        /// <returns>A completed task</returns>
        private async Task ClientReady()
        {
            try
            {
                await ApplicationWindow.ChangeBotConnectionButton("Connected", new Microsoft.Maui.Graphics.Color(0, 255, 0), new Microsoft.Maui.Graphics.Color(0, 0, 0));
                await ApplicationWindow.ToggleBotTokenEnable(false);
                MenuApp.MainPage.BotConnectionButtonInstance.BackgroundColor = new Microsoft.Maui.Graphics.Color(0, 255, 0);

                foreach (SocketGuild guild in DiscordClient.Guilds)
                {
                    try
                    {
                        var commandBuilder = new SlashCommandBuilder()
                                                .WithName("slackord")
                                                .WithDescription("Posts all parsed Slack JSON messages to the text channel the command came from.");

                        await guild.CreateApplicationCommandAsync(commandBuilder.Build());

                        var resumeCommandBuilder = new SlashCommandBuilder()
                            .WithName("resume")
                            .WithDescription("Resume importing messages to a channel.");

                        await guild.CreateApplicationCommandAsync(resumeCommandBuilder.Build());
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

        /// <summary>
        /// Handles the resume slash command to continue incomplete import sessions
        /// </summary>
        /// <param name="command">The resume slash command</param>
        /// <returns>A completed task</returns>
        private async Task HandleResumeCommandAsync(SocketSlashCommand command)
        {
            var incompleteSessions = ImportSession.GetIncompleteImports();

            if (incompleteSessions.Count == 0)
            {
                await command.RespondAsync("❌ No incomplete imports found.");
                return;
            }

            var sessionToResume = incompleteSessions.First();
            ImportJson.SetCurrentSession(sessionToResume);

            var incompleteChannels = sessionToResume.Channels.Where(c => !c.IsCompleted).ToList();

            if (incompleteChannels.Count == 0)
            {
                await command.RespondAsync("❌ No incomplete channels found in the most recent session.");
                return;
            }

            int totalRemainingMessages = incompleteChannels.Sum(c => c.MessagesRemaining);

            Application.Current.Dispatcher.Dispatch(() => {
                ApplicationWindow.WriteToDebugWindow($"🔄 Resuming session: {sessionToResume.SessionId}\n");
                ApplicationWindow.WriteToDebugWindow($"📋 Incomplete channels: {incompleteChannels.Count}\n");
                ApplicationWindow.WriteToDebugWindow($"📨 Messages remaining: {totalRemainingMessages:N0}\n\n");
            });

            await command.RespondAsync($"✅ Resuming import session `{sessionToResume.SessionId}`\n" +
                                     $"📁 Channels: {incompleteChannels.Count} incomplete\n" +
                                     $"📨 Messages: {totalRemainingMessages:N0} remaining");

            ulong? guildId = command.GuildId;
            await PostMessagesToDiscord((ulong)guildId, command, isResume: true);
        }

        /// <summary>
        /// Posts messages from the current import session to Discord channels
        /// </summary>
        /// <param name="guildID">The Discord guild ID where messages will be posted</param>
        /// <param name="interaction">The Discord interaction that triggered this operation</param>
        /// <param name="isResume">Whether this is resuming an existing import</param>
        /// <returns>A completed task</returns>
        [SlashCommand("slackord", "Posts all parsed Slack JSON messages to the text channel the command came from.")]
        public async Task PostMessagesToDiscord(ulong guildID, SocketInteraction interaction, bool isResume = false)
        {
            var currentSession = ImportJson.GetCurrentSession();

            if (currentSession == null)
            {
                await interaction.RespondAsync("❌ No import session found. Please run an import first.");
                return;
            }

            if (!ProcessingManager.Instance.CanStartDiscordImport && !isResume)
            {
                await interaction.RespondAsync($"❌ Cannot start Discord import. Current state: {ProcessingManager.GetDisplayText(ProcessingManager.Instance.CurrentState)}");
                return;
            }

            ProcessingManager.Instance.SetState(ProcessingState.ImportingToDiscord);

            _discordOperationsCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _discordOperationsCancellationTokenSource.Token;

            var channelsToProcess = isResume
                ? currentSession.Channels.Where(c => !c.IsCompleted).ToList()
                : [.. currentSession.Channels];

            int totalMessagesToPost = channelsToProcess.Sum(c => c.TotalMessages - c.MessagesSent);
            int totalMessagesAcrossAllChannels = currentSession.Channels.Sum(c => c.TotalMessages);
            int totalMessagesSentPreviously = currentSession.Channels.Sum(c => c.MessagesSent);

            Application.Current.Dispatcher.Dispatch(() => {
                ApplicationWindow.WriteToDebugWindow($"📤 Starting Discord import for session {currentSession.SessionId}\n");
                ApplicationWindow.WriteToDebugWindow($"📊 Channels to process: {channelsToProcess.Count}\n");
                ApplicationWindow.WriteToDebugWindow($"📋 Messages to post: {totalMessagesToPost:N0}\n");
                if (isResume)
                {
                    ApplicationWindow.WriteToDebugWindow($"📈 Progress: {totalMessagesSentPreviously:N0}/{totalMessagesAcrossAllChannels:N0} messages already sent\n");
                }
                ApplicationWindow.WriteToDebugWindow($"\n");
            });

            try
            {
                if (!isResume)
                {
                    await interaction.DeferAsync();
                }

                cancellationToken.ThrowIfCancellationRequested();
                await CreateDiscordChannelsForSession(guildID, currentSession, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                ProcessingManager.Instance.SetState(ProcessingState.Error);
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"🛑 Operation cancelled during channel setup.\n"); });

                if (!isResume)
                    await interaction.FollowupAsync("❌ Message posting was cancelled.");
                else
                    await interaction.FollowupAsync("❌ Resume operation was cancelled.");

                await ApplicationWindow.OnOperationCancelled();
                return;
            }
            catch (Exception ex)
            {
                ProcessingManager.Instance.SetState(ProcessingState.Error);
                Application.Current.Dispatcher.Dispatch(() => { ApplicationWindow.WriteToDebugWindow($"Error: {ex.Message}\n"); });

                if (!isResume)
                    await interaction.FollowupAsync($"❌ Error setting up channels: {ex.Message}");
                else
                    await interaction.FollowupAsync($"❌ Error during resume: {ex.Message}");
                return;
            }

            SocketGuild guild = DiscordClient.GetGuild(guildID);
            long fileSizeLimit = guild.PremiumTier switch
            {
                PremiumTier.Tier1 => 8 * 1024 * 1024,
                PremiumTier.Tier2 => 50 * 1024 * 1024,
                PremiumTier.Tier3 => 100 * 1024 * 1024,
                _ => 8 * 1024 * 1024
            };

            Dictionary<string, IThreadChannel> threadStartsDict = [];
            int messagesPosted = 0;
            int cumulativeMessagesPosted = totalMessagesSentPreviously;
            bool errorOccurred = false;
            bool wasCancelled = false;
            ApplicationWindow.ResetProgressBar();

            if (isResume && totalMessagesSentPreviously > 0)
            {
                ApplicationWindow.UpdateProgressBar(totalMessagesSentPreviously, totalMessagesAcrossAllChannels, "messages");
            }

            try
            {
                foreach (var channelProgress in channelsToProcess)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (CreatedChannels.TryGetValue(channelProgress.DiscordChannelId, out ITextChannel discordChannel))
                    {
                        try
                        {
                            int channelMessagesPosted = await ProcessChannelMessages(channelProgress, discordChannel, currentSession, threadStartsDict,
                                fileSizeLimit, messagesPosted, totalMessagesAcrossAllChannels, totalMessagesSentPreviously, cancellationToken);

                            messagesPosted += channelMessagesPosted;
                            cumulativeMessagesPosted += channelMessagesPosted;
                        }
                        catch (OperationCanceledException)
                        {
                            int currentSessionProgress = currentSession.Channels.Sum(c => c.MessagesSent) - totalMessagesSentPreviously;
                            cumulativeMessagesPosted = totalMessagesSentPreviously + currentSessionProgress;
                            throw;
                        }
                    }
                    else
                    {
                        Application.Current.Dispatcher.Dispatch(() => {
                            ApplicationWindow.WriteToDebugWindow($"❌ Discord channel not found for: {channelProgress.Name}\n");
                        });
                    }

                    if (errorOccurred || wasCancelled)
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
                int currentSessionProgress = currentSession.Channels.Sum(c => c.MessagesSent) - totalMessagesSentPreviously;
                cumulativeMessagesPosted = totalMessagesSentPreviously + currentSessionProgress;

                Application.Current.Dispatcher.Dispatch(() =>
                {
                    ApplicationWindow.WriteToDebugWindow($"🛑 Operation cancelled after posting {currentSessionProgress} messages in this session ({cumulativeMessagesPosted:N0} total)\n");
                });
            }
            catch (Exception ex)
            {
                errorOccurred = true;
                int currentSessionProgress = currentSession.Channels.Sum(c => c.MessagesSent) - totalMessagesSentPreviously;
                cumulativeMessagesPosted = totalMessagesSentPreviously + currentSessionProgress;

                Application.Current.Dispatcher.Dispatch(() => {
                    ApplicationWindow.WriteToDebugWindow($"❌ Unexpected error during message posting: {ex.Message}\n");
                });
            }

            if (wasCancelled)
            {
                ProcessingManager.Instance.SetState(ProcessingState.Error);
                currentSession.Save();

                int currentSessionProgress = currentSession.Channels.Sum(c => c.MessagesSent) - totalMessagesSentPreviously;

                if (!isResume)
                    await interaction.FollowupAsync($"❌ Message posting was cancelled after {currentSessionProgress} messages.");
                else
                    await interaction.FollowupAsync($"❌ Resume operation was cancelled after {currentSessionProgress} messages.");

                await ApplicationWindow.OnOperationCancelled();
            }
            else if (!errorOccurred)
            {
                ProcessingManager.Instance.SetState(ProcessingState.Completed);
                currentSession.Save();

                if (!isResume)
                    await interaction.FollowupAsync("✅ All messages sent to Discord successfully!");
                else
                    await interaction.FollowupAsync("✅ Resume completed successfully!");

                Application.Current.Dispatcher.Dispatch(() => {
                    ApplicationWindow.WriteToDebugWindow($"🎉 Import completed successfully!\n");
                    ApplicationWindow.WriteToDebugWindow($"📊 Final stats: {cumulativeMessagesPosted:N0} total messages posted\n\n");
                });
            }
            else
            {
                ProcessingManager.Instance.SetState(ProcessingState.Error);
                currentSession.Save();

                int currentSessionProgress = currentSession.Channels.Sum(c => c.MessagesSent) - totalMessagesSentPreviously;

                if (!isResume)
                    await interaction.FollowupAsync($"❌ Message sending was interrupted after {currentSessionProgress} messages.");
                else
                    await interaction.FollowupAsync($"❌ Resume operation was interrupted after {currentSessionProgress} messages.");
            }
        }

        /// <summary>
        /// Processes and posts messages for a specific channel
        /// </summary>
        /// <param name="channelProgress">The channel progress tracker</param>
        /// <param name="discordChannel">The Discord channel to post to</param>
        /// <param name="session">The current import session</param>
        /// <param name="threadStartsDict">Dictionary tracking thread starts</param>
        /// <param name="fileSizeLimit">Maximum file size for uploads</param>
        /// <param name="currentSessionMessagesPosted">Messages posted in current session</param>
        /// <param name="totalMessagesAcrossAllChannels">Total messages across all channels</param>
        /// <param name="totalMessagesSentPreviously">Messages sent in previous sessions</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The number of messages posted for this channel</returns>
        private static async Task<int> ProcessChannelMessages(ChannelProgress channelProgress, ITextChannel discordChannel,
            ImportSession session, Dictionary<string, IThreadChannel> threadStartsDict, long fileSizeLimit,
            int currentSessionMessagesPosted, int totalMessagesAcrossAllChannels, int totalMessagesSentPreviously, CancellationToken cancellationToken)
        {
            int localMessagesPosted = 0;

            try
            {
                Application.Current.Dispatcher.Dispatch(() => {
                    ApplicationWindow.WriteToDebugWindow($"📤 Processing channel: {channelProgress.Name} ({channelProgress.GetProgressDisplay()})\n");
                });

                string channelFilePath = session.GetChannelFilePath(channelProgress.Name);
                var allMessages = await SlackordFileManager.LoadChannelMessagesAsync(channelFilePath);

                if (allMessages.Count == 0)
                {
                    Application.Current.Dispatcher.Dispatch(() => {
                        ApplicationWindow.WriteToDebugWindow($"⚠️ No messages found for {channelProgress.Name}\n");
                    });
                    return 0;
                }

                var messagesToSend = allMessages.Skip(channelProgress.MessagesSent).ToList();

                if (messagesToSend.Count == 0)
                {
                    Application.Current.Dispatcher.Dispatch(() => {
                        ApplicationWindow.WriteToDebugWindow($"✅ {channelProgress.Name} already completed\n");
                    });
                    channelProgress.IsCompleted = true;
                    return 0;
                }

                var webhook = await discordChannel.CreateWebhookAsync("Slackord Temp Webhook");
                string webhookUrl = $"https://discord.com/api/webhooks/{webhook.Id}/{webhook.Token}";

                try
                {
                    using var webhookClient = new DiscordWebhookClient(webhookUrl);

                    foreach (var message in messagesToSend)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            await PostSingleMessage(message, webhookClient, discordChannel, threadStartsDict, fileSizeLimit, cancellationToken);

                            channelProgress.RecordMessageSent(message.OriginalTimestamp);
                            localMessagesPosted++;

                            if ((currentSessionMessagesPosted + localMessagesPosted) % 25 == 0)
                            {
                                session.Save();
                            }

                            int cumulativeProgress = totalMessagesSentPreviously + currentSessionMessagesPosted + localMessagesPosted;
                            ApplicationWindow.UpdateProgressBar(cumulativeProgress, totalMessagesAcrossAllChannels, "messages");

                            await Task.Delay(50, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Dispatch(() => {
                                ApplicationWindow.WriteToDebugWindow($"❌ Error posting message in {channelProgress.Name}: {ex.Message}\n");
                            });

                            bool shouldContinue = await MainPage.Current.DisplayAlert(
                                "Error Posting Message",
                                $"An error occurred while posting a message to {channelProgress.Name}: {ex.Message}\n\nWould you like to continue with the next message?",
                                "Continue", "Stop");

                            if (!shouldContinue)
                            {
                                session.Save();
                                throw new Exception("User chose to stop import");
                            }

                            channelProgress.RecordMessageSent(message.OriginalTimestamp);
                            localMessagesPosted++;

                            int cumulativeProgress = totalMessagesSentPreviously + currentSessionMessagesPosted + localMessagesPosted;
                            ApplicationWindow.UpdateProgressBar(cumulativeProgress, totalMessagesAcrossAllChannels, "messages");
                        }
                    }
                }
                finally
                {
                    try
                    {
                        await webhook.DeleteAsync();
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Dispatch(() => {
                            ApplicationWindow.WriteToDebugWindow($"⚠️ Error deleting webhook: {ex.Message}\n");
                        });
                    }
                }

                if (channelProgress.IsCompleted)
                {
                    Application.Current.Dispatcher.Dispatch(() => {
                        ApplicationWindow.WriteToDebugWindow($"✅ Completed channel: {channelProgress.Name}\n");
                    });
                }

                session.Save();
                return localMessagesPosted;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Dispatch(() => {
                    ApplicationWindow.WriteToDebugWindow($"❌ Error processing channel {channelProgress.Name}: {ex.Message}\n");
                });
                throw;
            }
        }

        /// <summary>
        /// Posts a single message to Discord with proper thread and attachment handling
        /// </summary>
        /// <param name="message">The reconstructed message to post</param>
        /// <param name="webhookClient">The Discord webhook client</param>
        /// <param name="discordChannel">The Discord channel to post to</param>
        /// <param name="threadStartsDict">Dictionary tracking thread starts</param>
        /// <param name="fileSizeLimit">Maximum file size for uploads</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A completed task</returns>
        private static async Task PostSingleMessage(ReconstructedMessage message, DiscordWebhookClient webhookClient,
            ITextChannel discordChannel, Dictionary<string, IThreadChannel> threadStartsDict,
            long fileSizeLimit, CancellationToken cancellationToken)
        {
            ulong? threadIdForReply = null;
            bool shouldArchiveThreadBack = false;

            if (message.ThreadType == ThreadType.Parent)
            {
                string threadName = string.IsNullOrEmpty(message.Message) ? "Replies" :
                    message.Message.Length <= 20 ? message.Message : message.Message[..20];

                await webhookClient.SendMessageAsync(message.Content, false, null, message.User, message.Avatar,
                    options: new RequestOptions { CancelToken = cancellationToken });

                cancellationToken.ThrowIfCancellationRequested();
                IEnumerable<IMessage> threadMessages = await discordChannel.GetMessagesAsync(1).FlattenAsync();

                cancellationToken.ThrowIfCancellationRequested();
                IThreadChannel threadID = await discordChannel.CreateThreadAsync(threadName, Discord.ThreadType.PublicThread,
                    ThreadArchiveDuration.OneHour, threadMessages.First(), options: new RequestOptions { CancelToken = cancellationToken });
                threadStartsDict[message.ParentThreadTs] = threadID;
            }
            else if (message.ThreadType == ThreadType.Reply)
            {
                if (threadStartsDict.TryGetValue(message.ParentThreadTs, out IThreadChannel threadID))
                {
                    if (threadID.IsArchived)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await threadID.ModifyAsync(properties => properties.Archived = false,
                            options: new RequestOptions { CancelToken = cancellationToken });
                        shouldArchiveThreadBack = true;
                    }
                    threadIdForReply = threadID.Id;
                }

                cancellationToken.ThrowIfCancellationRequested();
                await webhookClient.SendMessageAsync(message.Content, false, null, message.User, message.Avatar,
                    threadId: threadIdForReply, options: new RequestOptions { CancelToken = cancellationToken });

                if (shouldArchiveThreadBack && threadID != null)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await threadID.ModifyAsync(properties => properties.Archived = true,
                            options: new RequestOptions { CancelToken = cancellationToken });
                    }
                    catch (Exception archiveEx)
                    {
                        Application.Current.Dispatcher.Dispatch(() => {
                            ApplicationWindow.WriteToDebugWindow($"⚠️ Error archiving thread: {archiveEx.Message}\n");
                        });
                    }
                }
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                await webhookClient.SendMessageAsync(message.Content, false, null, message.User, message.Avatar,
                    options: new RequestOptions { CancelToken = cancellationToken });
            }

            await HandleMessageExtras(message, discordChannel, fileSizeLimit, cancellationToken);
        }

        /// <summary>
        /// Handles message pinning and file uploads for a posted message
        /// </summary>
        /// <param name="message">The reconstructed message with extras to handle</param>
        /// <param name="discordChannel">The Discord channel the message was posted to</param>
        /// <param name="fileSizeLimit">Maximum file size for uploads</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A completed task</returns>
        private static async Task HandleMessageExtras(ReconstructedMessage message, ITextChannel discordChannel,
            long fileSizeLimit, CancellationToken cancellationToken)
        {
            if (message.IsPinned && message.ThreadType == ThreadType.None)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    IEnumerable<IMessage> recentMessages = await discordChannel.GetMessagesAsync(1).Flatten().ToListAsync(cancellationToken);
                    IMessage recentMessage = recentMessages.FirstOrDefault();
                    if (recentMessage is IUserMessage userMessage)
                    {
                        await userMessage.PinAsync(options: new RequestOptions { CancelToken = cancellationToken });
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Dispatch(() => {
                        ApplicationWindow.WriteToDebugWindow($"⚠️ Error pinning message: {ex.Message}\n");
                    });
                }
            }

            for (int i = 0; i < message.FileURLs.Count; i++)
            {
                string localFilePath = message.FileURLs[i];
                cancellationToken.ThrowIfCancellationRequested();

                if (File.Exists(localFilePath))
                {
                    FileInfo fileInfo = new(localFilePath);
                    long fileSizeInBytes = fileInfo.Length;

                    if (fileSizeInBytes <= fileSizeLimit)
                    {
                        try
                        {
                            using FileStream fs = new(localFilePath, FileMode.Open, FileAccess.Read);
                            await discordChannel.SendFileAsync(fs, Path.GetFileName(localFilePath),
                                options: new RequestOptions { CancelToken = cancellationToken });
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"Failed to upload file {localFilePath}: {ex.Message}");
                            Application.Current.Dispatcher.Dispatch(() => {
                                ApplicationWindow.WriteToDebugWindow($"⚠️ Failed to upload file: {Path.GetFileName(localFilePath)}\n");
                            });
                        }
                    }
                    else
                    {
                        await discordChannel.SendMessageAsync($"📎 File too large to upload: {Path.GetFileName(localFilePath)} ({SlackordFileManager.GetFileSizeDisplay(localFilePath)})",
                            options: new RequestOptions { CancelToken = cancellationToken });
                    }
                }
                else
                {
                    Logger.Log($"File not found: {localFilePath}");
                    await discordChannel.SendMessageAsync($"📎 Attachment not found: {Path.GetFileName(localFilePath)}",
                        options: new RequestOptions { CancelToken = cancellationToken });

                    if (i < message.FallbackFileURLs.Count)
                    {
                        string url = message.FallbackFileURLs[i];
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            await discordChannel.SendMessageAsync(url,
                                options: new RequestOptions { CancelToken = cancellationToken });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates Discord channels for all channels in the import session
        /// </summary>
        /// <param name="guildID">The Discord guild ID to create channels in</param>
        /// <param name="session">The import session containing channel information</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A completed task</returns>
        private async Task CreateDiscordChannelsForSession(ulong guildID, ImportSession session, CancellationToken cancellationToken)
        {
            try
            {
                SocketGuild guild = DiscordClient.GetGuild(guildID);
                string baseCategoryName = "Slackord Import";

                foreach (var channelProgress in session.Channels.Where(c => c.DiscordChannelId > 0))
                {
                    var existingChannel = guild.GetTextChannel(channelProgress.DiscordChannelId);
                    if (existingChannel != null)
                    {
                        CreatedChannels[channelProgress.DiscordChannelId] = existingChannel;
                        Application.Current.Dispatcher.Dispatch(() => {
                            ApplicationWindow.WriteToDebugWindow($"🔗 Found existing Discord channel: #{existingChannel.Name} (ID: {channelProgress.DiscordChannelId})\n");
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Dispatch(() => {
                            ApplicationWindow.WriteToDebugWindow($"⚠️ Could not find Discord channel with ID {channelProgress.DiscordChannelId} for {channelProgress.Name}\n");
                        });
                        channelProgress.SetDiscordChannelId(0);
                    }
                }

                var channelsNeedingCreation = session.Channels.Where(c => c.NeedsDiscordChannel).ToList();

                if (channelsNeedingCreation.Count == 0)
                {
                    Application.Current.Dispatcher.Dispatch(() => {
                        ApplicationWindow.WriteToDebugWindow($"✅ All Discord channels already exist. Ready to resume posting.\n");
                    });
                    return;
                }

                Application.Current.Dispatcher.Dispatch(() => {
                    ApplicationWindow.WriteToDebugWindow($"📁 Creating {channelsNeedingCreation.Count} new Discord channels...\n");
                });

                SocketCategoryChannel currentCategory = guild.CategoryChannels.FirstOrDefault(c => c.Name.StartsWith(baseCategoryName));
                ulong currentCategoryId;

                if (currentCategory == null)
                {
                    var createdCategory = await guild.CreateCategoryChannelAsync(baseCategoryName);
                    currentCategoryId = createdCategory.Id;
                }
                else
                {
                    currentCategoryId = currentCategory.Id;
                }

                int channelCountInCurrentCategory = guild.Channels.Count(c => c is SocketTextChannel textChannel && textChannel.CategoryId == currentCategoryId);

                foreach (var channelProgress in channelsNeedingCreation)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (channelCountInCurrentCategory >= 50)
                    {
                        var newCategory = await guild.CreateCategoryChannelAsync($"{baseCategoryName} {DateTime.Now:MMdd-HHmm}");
                        currentCategoryId = newCategory.Id;
                        channelCountInCurrentCategory = 0;
                    }

                    string channelName = channelProgress.Name.ToLower();

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
                        properties.Topic = channelProgress.Description;
                    });

                    ulong createdChannelId = createdChannel.Id;
                    channelProgress.SetDiscordChannelId(createdChannelId);
                    CreatedChannels[createdChannelId] = createdChannel;

                    channelCountInCurrentCategory++;

                    Application.Current.Dispatcher.Dispatch(() => {
                        ApplicationWindow.WriteToDebugWindow($"📁 Created Discord channel: #{channelName} (ID: {createdChannelId})\n");
                    });
                }

                session.Save();
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"❌ CreateDiscordChannelsForSession: {ex.Message}\n");
                throw;
            }
        }
    }
}