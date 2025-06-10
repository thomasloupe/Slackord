using Slackord.Pages;

namespace Slackord.Classes
{
    /// <summary>
    /// Utility class for handling cleanup of import and download files after successful Discord import
    /// Optimized for thread safety and heavy I/O operations
    /// </summary>
    public static class ImportCleanupUtility
    {
        /// <summary>
        /// Handles post-import cleanup based on user preferences for a completed session
        /// This method properly handles thread marshalling for UI operations
        /// </summary>
        /// <param name="completedSession">The completed import session containing all channel information</param>
        public static async Task HandlePostImportCleanup(ImportSession completedSession)
        {
            if (completedSession == null || !completedSession.IsCompleted)
            {
                ApplicationWindow.WriteToDebugWindow("⚠️ Cannot cleanup - session is not completed\n");
                return;
            }

            var cleanupBehavior = OptionsPage.GetCleanupBehavior();

            try
            {
                var cleanupInfo = await Task.Run(() => CalculateCleanupSize(completedSession));

                switch (cleanupBehavior)
                {
                    case OptionsPage.CleanupBehavior.Prompt:
                        await HandlePromptCleanupThreadSafe(cleanupInfo, completedSession);
                        break;

                    case OptionsPage.CleanupBehavior.Automatically:
                        await HandleAutomaticCleanupThreadSafe(cleanupInfo, completedSession);
                        break;

                    case OptionsPage.CleanupBehavior.Never:
                        HandleNeverCleanup(cleanupInfo);
                        break;
                }
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"❌ Error during post-import cleanup: {ex.Message}\n");
                Logger.Log($"ImportCleanupUtility.HandlePostImportCleanup: {ex}");
            }
        }

        /// <summary>
        /// Thread-safe version of prompt cleanup that properly marshals UI operations
        /// </summary>
        /// <param name="cleanupInfo">Information about files to be cleaned up</param>
        /// <param name="session">The completed import session</param>
        private static async Task HandlePromptCleanupThreadSafe(CleanupInfo cleanupInfo, ImportSession session)
        {
            if (cleanupInfo.TotalSizeBytes == 0)
            {
                ApplicationWindow.WriteToDebugWindow("ℹ️ No cleanup needed - no files found to clean up\n");
                return;
            }

            string sizeText = FormatFileSize(cleanupInfo.TotalSizeBytes);
            string channelList = cleanupInfo.ChannelsWithDownloads.Count > 0
                ? $"\n• Downloads from {cleanupInfo.ChannelsWithDownloads.Count} channels: {string.Join(", ", cleanupInfo.ChannelsWithDownloads.Take(5))}"
                : "";

            if (cleanupInfo.ChannelsWithDownloads.Count > 5)
            {
                channelList += $" (and {cleanupInfo.ChannelsWithDownloads.Count - 5} more)";
            }

            string message = $"🎉 Import completed successfully!\n\n" +
                           $"We can clean up the temporary files to reclaim {sizeText} of disk space.\n\n" +
                           $"Files to remove:\n" +
                           $"• Import session folder: {session.SessionId}\n" +
                           $"  - {cleanupInfo.ImportFilesCount} data files{channelList}\n" +
                           $"• {cleanupInfo.DownloadFilesCount} downloaded files\n\n" +
                           $"Would you like to clean up these files?";

            try
            {
                bool shouldCleanup = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        if (Application.Current?.Windows?.Count == 0 || Application.Current.Windows[0].Page == null)
                        {
                            return false;
                        }

                        return await Application.Current.Windows[0].Page.DisplayAlert(
                            "Import Complete - Cleanup Available",
                            message,
                            "Yes, clean up",
                            "No, keep files"
                        );
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Error showing cleanup dialog: {ex.Message}");
                        return false;
                    }
                });

                if (shouldCleanup)
                {
                    await Task.Run(() => PerformCleanupSync(session, cleanupInfo.ChannelsWithDownloads));
                    ApplicationWindow.WriteToDebugWindow($"✅ Cleanup completed! Reclaimed {sizeText} of disk space.\n");
                }
                else
                {
                    ApplicationWindow.WriteToDebugWindow($"💾 Import completed. You can manually clean up the import session '{session.SessionId}' and downloads to reclaim {sizeText}.\n");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in cleanup prompt handling: {ex.Message}");
                ApplicationWindow.WriteToDebugWindow($"ℹ️ Import completed. You can manually clean up files to reclaim {sizeText}.\n");
            }
        }

        /// <summary>
        /// Thread-safe version of automatic cleanup
        /// </summary>
        /// <param name="cleanupInfo">Information about files to be cleaned up</param>
        /// <param name="session">The completed import session</param>
        private static async Task HandleAutomaticCleanupThreadSafe(CleanupInfo cleanupInfo, ImportSession session)
        {
            if (cleanupInfo.TotalSizeBytes == 0)
            {
                ApplicationWindow.WriteToDebugWindow("ℹ️ No cleanup needed - no files found to clean up\n");
                return;
            }

            string sizeText = FormatFileSize(cleanupInfo.TotalSizeBytes);
            await Task.Run(() => PerformCleanupSync(session, cleanupInfo.ChannelsWithDownloads));
            ApplicationWindow.WriteToDebugWindow($"🧹 Auto-cleanup completed! Removed session '{session.SessionId}' and {cleanupInfo.ImportFilesCount + cleanupInfo.DownloadFilesCount} files, reclaimed {sizeText} of disk space.\n");
        }

        /// <summary>
        /// Calculates the total size of files that would be cleaned up for a session
        /// This method is optimized for background thread execution
        /// </summary>
        /// <param name="session">The import session to analyze</param>
        /// <returns>Cleanup information including total size and file counts</returns>
        private static CleanupInfo CalculateCleanupSize(ImportSession session)
        {
            var info = new CleanupInfo();

            try
            {
                if (Directory.Exists(session.SessionPath))
                {
                    var importFiles = Directory.GetFiles(session.SessionPath, "*", SearchOption.AllDirectories);
                    info.ImportFilesCount = importFiles.Length;
                    info.ImportSizeBytes = importFiles.Sum(file => GetFileSizeSafe(file));
                }

                string downloadsBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

                if (Directory.Exists(downloadsBasePath))
                {
                    try
                    {
                        var channelNames = session.Channels.Select(c => c.Name.ToLowerInvariant()).ToHashSet();
                        var downloadDirectories = Directory.GetDirectories(downloadsBasePath);

                        foreach (string downloadDir in downloadDirectories)
                        {
                            string folderName = Path.GetFileName(downloadDir).ToLowerInvariant();

                            bool isChannelDownload = channelNames.Any(channelName =>
                                folderName == channelName ||
                                folderName.Contains(channelName) ||
                                channelName.Contains(folderName));

                            if (!isChannelDownload)
                            {
                                isChannelDownload = folderName.StartsWith("slackord") ||
                                                  folderName.EndsWith("-channel") ||
                                                  (folderName.Contains("-") && channelNames.Any(cn =>
                                                      folderName.Contains(cn.Replace("_", "-").Replace(" ", "-"))));
                            }

                            if (isChannelDownload)
                            {
                                try
                                {
                                    var downloadFiles = Directory.GetFiles(downloadDir, "*", SearchOption.AllDirectories);
                                    if (downloadFiles.Length > 0)
                                    {
                                        info.DownloadFilesCount += downloadFiles.Length;
                                        info.DownloadSizeBytes += downloadFiles.Sum(GetFileSizeSafe);
                                        info.ChannelsWithDownloads.Add(Path.GetFileName(downloadDir));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log($"Error calculating size for download folder {downloadDir}: {ex.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Error scanning downloads folder: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error calculating cleanup size: {ex.Message}");
            }

            return info;
        }

        /// <summary>
        /// Safely gets file size without throwing exceptions
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>File size in bytes or 0 if error</returns>
        private static long GetFileSizeSafe(string filePath)
        {
            try
            {
                return new FileInfo(filePath).Length;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Synchronous version of cleanup for background thread execution
        /// </summary>
        /// <param name="session">The import session to clean up</param>
        /// <param name="channelsWithDownloads">List of actual download folder names found during size calculation</param>
        private static void PerformCleanupSync(ImportSession session, List<string> channelsWithDownloads)
        {
            try
            {
                if (Directory.Exists(session.SessionPath))
                {
                    Directory.Delete(session.SessionPath, true);
                    Logger.Log($"Deleted import session folder: {session.SessionPath}");
                }

                string downloadsBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                foreach (string folderName in channelsWithDownloads)
                {
                    string downloadPath = Path.Combine(downloadsBasePath, folderName);
                    if (Directory.Exists(downloadPath))
                    {
                        try
                        {
                            Directory.Delete(downloadPath, true);
                            Logger.Log($"Deleted download folder: {downloadPath}");
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"Error deleting download folder {downloadPath}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error during cleanup: {ex.Message}");
                ApplicationWindow.WriteToDebugWindow($"⚠️ Some files could not be cleaned up: {ex.Message}\n");
            }
        }

        /// <summary>
        /// Handles cleanup when user preference is set to never clean up
        /// </summary>
        /// <param name="cleanupInfo">Information about files that could be cleaned up</param>
        private static void HandleNeverCleanup(CleanupInfo cleanupInfo)
        {
            if (cleanupInfo.TotalSizeBytes == 0)
            {
                ApplicationWindow.WriteToDebugWindow("ℹ️ Import completed successfully.\n");
                return;
            }

            string sizeText = FormatFileSize(cleanupInfo.TotalSizeBytes);
            ApplicationWindow.WriteToDebugWindow($"💾 Import completed successfully. {sizeText} of import data and downloads remain available for manual cleanup if needed.\n");
            ApplicationWindow.WriteToDebugWindow($"💡 Files are located in the Imports folder and Downloads folder.\n");
        }

        /// <summary>
        /// Formats a byte count into a human-readable size string
        /// </summary>
        /// <param name="bytes">Number of bytes to format</param>
        /// <returns>Formatted size string (e.g., "1.5 GB", "250 MB")</returns>
        private static string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";

            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F1} {suffixes[suffixIndex]}";
        }

        /// <summary>
        /// Gets a summary of cleanup potential for display purposes
        /// </summary>
        /// <param name="session">The import session to analyze</param>
        /// <returns>A formatted string describing cleanup potential</returns>
        public static async Task<string> GetCleanupSummaryAsync(ImportSession session)
        {
            if (session == null || !session.IsCompleted)
                return "Session not ready for cleanup";

            try
            {
                var cleanupInfo = await Task.Run(() => CalculateCleanupSize(session));
                if (cleanupInfo.TotalSizeBytes == 0)
                    return "No files to clean up";

                string sizeText = FormatFileSize(cleanupInfo.TotalSizeBytes);
                return $"Can reclaim {sizeText} ({cleanupInfo.ImportFilesCount + cleanupInfo.DownloadFilesCount} files)";
            }
            catch (Exception ex)
            {
                Logger.Log($"Error getting cleanup summary: {ex.Message}");
                return "Error calculating cleanup size";
            }
        }

        /// <summary>
        /// Contains information about files that can be cleaned up
        /// </summary>
        private class CleanupInfo
        {
            /// <summary>
            /// Number of import data files in the session folder
            /// </summary>
            public int ImportFilesCount { get; set; }

            /// <summary>
            /// Number of downloaded files across all channels
            /// </summary>
            public int DownloadFilesCount { get; set; }

            /// <summary>
            /// Total size of import session files in bytes
            /// </summary>
            public long ImportSizeBytes { get; set; }

            /// <summary>
            /// Total size of download files in bytes
            /// </summary>
            public long DownloadSizeBytes { get; set; }

            /// <summary>
            /// List of channel names that have download folders
            /// </summary>
            public List<string> ChannelsWithDownloads { get; set; } = new List<string>();

            /// <summary>
            /// Total size of all files that can be cleaned up
            /// </summary>
            public long TotalSizeBytes => ImportSizeBytes + DownloadSizeBytes;
        }
    }
}