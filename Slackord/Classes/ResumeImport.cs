using MenuApp;

namespace Slackord.Classes
{
    public class ResumeImport
    {
        public static async Task CheckForPartialImport()
        {
            // Check if resume is enabled in settings
            bool enableResumeImport = Preferences.Default.Get("EnableResumeImport", true);
            if (!enableResumeImport)
            {
                return;
            }

            // Check if there's a partial import to resume
            bool hasPartialImport = Preferences.Default.Get("HasPartialImport", false);
            if (!hasPartialImport)
            {
                return;
            }

            // Get the import details
            string lastImportType = Preferences.Default.Get("LastImportType", string.Empty);
            string lastImportChannel = Preferences.Default.Get("LastImportChannel", string.Empty);

            if (string.IsNullOrEmpty(lastImportType))
            {
                return;
            }

            // Ask the user if they want to resume
            bool shouldResume = await MainPage.Current.DisplayAlert(
                "Resume Import",
                $"A previous {(lastImportType == "Full" ? "server" : "channel")} import was interrupted during Discord posting. " +
                $"Would you like to resume from where it left off?\n\n" +
                $"Channel: {lastImportChannel}",
                "Resume", "Start New");

            if (shouldResume)
            {
                await ResumeImportProcess(lastImportType == "Full", lastImportChannel);
            }
            else
            {
                // Clear the resume state if user doesn't want to resume
                ClearResumeState();
                ApplicationWindow.WriteToDebugWindow("Previous import state cleared. Starting fresh.\n");
            }
        }

        private static async Task ResumeImportProcess(bool isFullExport, string channelName)
        {
            try
            {
                ApplicationWindow.WriteToDebugWindow($"🔄 Preparing to resume {(isFullExport ? "server" : "channel")} import...\n");

                // Clear the partial import state since we're handling it now
                ClearResumeState();

                // Show a clear message about what the user needs to do
                bool shouldProceed = await MainPage.Current.DisplayAlert(
                    "Resume Instructions",
                    $"To resume your import:\n\n" +
                    $"1. Click 'Import {(isFullExport ? "Server" : "Channel")}' button\n" +
                    $"2. Select the same folder you used before\n" +
                    $"3. Once processing is complete, use the Discord '/slackord' command\n\n" +
                    $"The system will automatically skip messages that were already posted.",
                    "OK", "Cancel");

                if (shouldProceed)
                {
                    ApplicationWindow.WriteToDebugWindow($"ℹ️ Ready to resume import. Please follow the instructions above.\n");
                    ApplicationWindow.WriteToDebugWindow($"💡 Tip: The system will automatically detect and skip already-posted messages.\n\n");
                }
                else
                {
                    ApplicationWindow.WriteToDebugWindow("Resume cancelled by user.\n");
                }
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"Error preparing resume: {ex.Message}\n");
                await MainPage.Current.DisplayAlert("Resume Error", $"An error occurred while preparing to resume: {ex.Message}", "OK");
            }
        }

        public static void ClearResumeState()
        {
            // Clear the resume state
            Preferences.Default.Set("LastImportType", string.Empty);
            Preferences.Default.Set("LastImportChannel", string.Empty);
            Preferences.Default.Set("LastSuccessfulMessageTimestamp", string.Empty);
            Preferences.Default.Set("HasPartialImport", false);
        }
    }
}