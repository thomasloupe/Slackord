namespace Slackord.Classes
{
    public enum ProcessingState
    {
        Idle,
        ImportingFiles,
        DeconstructingMessages,
        ReconstructingMessages,
        ReadyForDiscordImport,
        ImportingToDiscord,
        Completed,
        Error
    }

    public class ProcessingManager
    {
        private static ProcessingManager _instance;
        public static ProcessingManager Instance => _instance ??= new ProcessingManager();

        public ProcessingState CurrentState { get; private set; } = ProcessingState.Idle;
        public bool CanStartDiscordImport => CurrentState == ProcessingState.ReadyForDiscordImport;

        public event EventHandler<ProcessingState> StateChanged;

        public void SetState(ProcessingState newState)
        {
            CurrentState = newState;
            StateChanged?.Invoke(this, newState);

            // Update UI based on state
            Application.Current.Dispatcher.Dispatch(() =>
            {
                UpdateUIForState(newState);
            });
        }

        /// <summary>
        /// Converts the processing state to user-friendly display text
        /// </summary>
        public static string GetDisplayText(ProcessingState state)
        {
            return state switch
            {
                ProcessingState.Idle => "Ready",
                ProcessingState.ImportingFiles => "Importing Files",
                ProcessingState.DeconstructingMessages => "Processing Messages",
                ProcessingState.ReconstructingMessages => "Converting for Discord",
                ProcessingState.ReadyForDiscordImport => "Ready for Discord",
                ProcessingState.ImportingToDiscord => "Posting to Discord",
                ProcessingState.Completed => "Completed",
                ProcessingState.Error => "Error",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets user-friendly display text for the current state
        /// </summary>
        public string GetCurrentDisplayText() => GetDisplayText(CurrentState);

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