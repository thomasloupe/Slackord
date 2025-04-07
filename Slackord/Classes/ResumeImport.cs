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
            string lastMessageTimestamp = Preferences.Default.Get("LastSuccessfulMessageTimestamp", string.Empty);

            if (string.IsNullOrEmpty(lastImportType) || string.IsNullOrEmpty(lastMessageTimestamp))
            {
                return;
            }

            // Ask the user if they want to resume
            bool shouldResume = await MainPage.Current.DisplayAlert(
                "Resume Import",
                $"A previous {(lastImportType == "Full" ? "server" : "channel")} import was interrupted. Would you like to resume from where it left off?",
                "Resume", "Start New");

            if (shouldResume)
            {
                await ResumeImportProcess(lastImportType == "Full", lastImportChannel, lastMessageTimestamp);
            }
            else
            {
                // Clear the resume state if user doesn't want to resume
                ClearResumeState();
            }
        }

        private static async Task ResumeImportProcess(bool isFullExport, string channelName, string lastMessageTimestamp)
        {
            try
            {
                // Get the root folder path from the last import
                string rootFolderPath = Preferences.Default.Get("LastImportFolderPath", string.Empty);
                if (string.IsNullOrEmpty(rootFolderPath) || !Directory.Exists(rootFolderPath))
                {
                    // If we don't have the path or it doesn't exist, we need to ask the user to select it again
                    ApplicationWindow.WriteToDebugWindow("Previous import folder not found. Please select the import folder again.\n");
                    await new ApplicationWindow().ImportJsonAsync(isFullExport);
                    return;
                }

                // We can't set ImportJson.RootFolderPath directly, so we'll need to use ImportJsonAsync
                // and modify our approach to handle resuming
                ApplicationWindow.WriteToDebugWindow($"Resuming {(isFullExport ? "server" : "channel")} import from {rootFolderPath}...\n");

                // Start a new import process
                await new ApplicationWindow().ImportJsonAsync(isFullExport);

                // Now we need to identify where to resume from based on the timestamp
                foreach (Channel channel in ImportJson.Channels)
                {
                    // If we're resuming a specific channel import, make sure we're in the right channel
                    if (!isFullExport && channel.Name != channelName)
                    {
                        continue;
                    }

                    // Find the index of the last successfully sent message
                    int resumeIndex = -1;
                    for (int i = 0; i < channel.ReconstructedMessagesList.Count; i++)
                    {
                        if (channel.ReconstructedMessagesList[i].OriginalTimestamp == lastMessageTimestamp)
                        {
                            resumeIndex = i;
                            break;
                        }
                    }

                    if (resumeIndex >= 0)
                    {
                        // Remove all messages up to and including the last successful one
                        channel.ReconstructedMessagesList.RemoveRange(0, resumeIndex + 1);
                        ApplicationWindow.WriteToDebugWindow($"Resuming from message {resumeIndex + 1} in channel {channel.Name}\n");
                    }
                }

                // Log the total number of remaining messages
                int totalRemainingMessages = ImportJson.Channels.Sum(channel => channel.ReconstructedMessagesList.Count);
                ApplicationWindow.WriteToDebugWindow($"Found {totalRemainingMessages} messages to resume sending.\n");

                // Clear the partial import state now that we're resuming
                ClearResumeState();

                if (totalRemainingMessages > 0)
                {
                    // Let the user know we're ready to post the remaining messages
                    bool shouldPost = await MainPage.Current.DisplayAlert(
                        "Ready to Resume",
                        $"Ready to post {totalRemainingMessages} remaining messages. Use the Discord slash command to begin posting.",
                        "OK", "Cancel");

                    if (!shouldPost)
                    {
                        // If the user cancels, clear everything
                        ImportJson.Channels.Clear();
                    }
                }
                else
                {
                    await MainPage.Current.DisplayAlert(
                        "No Messages to Resume",
                        "There are no messages to resume posting. The import process will need to be started from the beginning.",
                        "OK");

                    // Clear everything for a clean slate
                    ImportJson.Channels.Clear();
                }
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"Error resuming import: {ex.Message}\n");
                await MainPage.Current.DisplayAlert("Resume Error", $"An error occurred while trying to resume: {ex.Message}", "OK");
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