namespace Slackord.Classes
{
    /// <summary>
    /// Defines the various states of the application's processing workflow
    /// </summary>
    public enum ProcessingState
    {
        /// <summary>
        /// Application is idle and ready for user input
        /// </summary>
        Idle,
        /// <summary>
        /// Currently importing JSON files from Slack export
        /// </summary>
        ImportingFiles,
        /// <summary>
        /// Processing and deconstructing Slack messages
        /// </summary>
        DeconstructingMessages,
        /// <summary>
        /// Converting messages for Discord compatibility
        /// </summary>
        ReconstructingMessages,
        /// <summary>
        /// Ready to begin Discord import process
        /// </summary>
        ReadyForDiscordImport,
        /// <summary>
        /// Currently posting messages to Discord
        /// </summary>
        ImportingToDiscord,
        /// <summary>
        /// All operations completed successfully
        /// </summary>
        Completed,
        /// <summary>
        /// An error occurred during processing
        /// </summary>
        Error
    }

    /// <summary>
    /// Manages the current state of processing operations and UI updates
    /// </summary>
    public class ProcessingManager
    {
        /// <summary>
        /// Singleton instance field
        /// </summary>
        private static ProcessingManager _instance;

        /// <summary>
        /// Gets the singleton instance of ProcessingManager
        /// </summary>
        public static ProcessingManager Instance => _instance ??= new ProcessingManager();

        /// <summary>
        /// Gets the current processing state
        /// </summary>
        public ProcessingState CurrentState { get; private set; } = ProcessingState.Idle;

        /// <summary>
        /// Gets whether Discord import can be started based on current state
        /// </summary>
        public bool CanStartDiscordImport => CurrentState == ProcessingState.ReadyForDiscordImport;

        /// <summary>
        /// Event raised when the processing state changes
        /// </summary>
        public event EventHandler<ProcessingState> StateChanged;

        /// <summary>
        /// Sets the current processing state and triggers UI updates
        /// </summary>
        /// <param name="newState">The new processing state to set</param>
        public void SetState(ProcessingState newState)
        {
            CurrentState = newState;
            StateChanged?.Invoke(this, newState);

            Application.Current.Dispatcher.Dispatch(() =>
            {
                UpdateUIForState(newState);
            });

            // Handle cleanup when import completes successfully
            if (newState == ProcessingState.Completed)
            {
                Application.Current.Dispatcher.Dispatch(async () =>
                {
                    try
                    {
                        var currentSession = ImportJson.GetCurrentSession();
                        if (currentSession != null && currentSession.IsCompleted)
                        {
                            await ImportCleanupUtility.HandlePostImportCleanup(currentSession);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Error during post-completion cleanup: {ex.Message}");
                        ApplicationWindow.WriteToDebugWindow($"⚠️ Post-completion cleanup encountered an issue: {ex.Message}\n");
                    }
                });
            }
        }

        /// <summary>
        /// Converts the processing state to user-friendly display text
        /// </summary>
        /// <param name="state">The processing state to convert</param>
        /// <returns>A human-readable description of the state</returns>
        public static string GetDisplayText(ProcessingState state)
        {
            return state switch
            {
                ProcessingState.Idle => "Ready",
                ProcessingState.ImportingFiles => "Importing Files",
                ProcessingState.DeconstructingMessages => "Processing Messages",
                ProcessingState.ReconstructingMessages => "Converting for Discord",
                ProcessingState.ReadyForDiscordImport => "Ready for Discord Import",
                ProcessingState.ImportingToDiscord => "Posting to Discord",
                ProcessingState.Completed => "Completed",
                ProcessingState.Error => "Error",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets user-friendly display text for the current state
        /// </summary>
        /// <returns>A human-readable description of the current state</returns>
        public string GetCurrentDisplayText() => GetDisplayText(CurrentState);

        /// <summary>
        /// Updates the UI based on the current processing state
        /// </summary>
        /// <param name="state">The processing state that was set</param>
        private static void UpdateUIForState(ProcessingState state)
        {
            switch (state)
            {
                case ProcessingState.ImportingFiles:
                    ApplicationWindow.WriteToDebugWindow("📁 Importing JSON files...\n");
                    break;
                case ProcessingState.DeconstructingMessages:
                    ApplicationWindow.WriteToDebugWindow("🔧 Deconstructing Slack messages...\n");
                    break;
                case ProcessingState.ReconstructingMessages:
                    ApplicationWindow.WriteToDebugWindow("🔨 Reconstructing for Discord...\n");
                    break;
                case ProcessingState.ReadyForDiscordImport:
                    ApplicationWindow.WriteToDebugWindow("✅ READY FOR DISCORD IMPORT! Use the Discord slash command to begin posting.\n");
                    ApplicationWindow.HideProgressBar();
                    break;
                case ProcessingState.ImportingToDiscord:
                    ApplicationWindow.WriteToDebugWindow("📤 Importing messages to Discord...\n");
                    break;
                case ProcessingState.Completed:
                    ApplicationWindow.WriteToDebugWindow("🎉 All operations completed successfully!\n");
                    ApplicationWindow.HideProgressBar();
                    break;
                case ProcessingState.Error:
                    ApplicationWindow.WriteToDebugWindow("❌ An error occurred during processing.\n");
                    ApplicationWindow.HideProgressBar();
                    break;
            }
        }
    }
}