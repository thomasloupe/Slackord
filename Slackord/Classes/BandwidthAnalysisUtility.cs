namespace Slackord.Classes
{
    /// <summary>
    /// Utility to analyze potential bandwidth savings from smart downloading
    /// </summary>
    public static class BandwidthAnalysisUtility
    {
        /// <summary>
        /// Analyzes what files would be downloaded vs skipped before actually downloading
        /// </summary>
        /// <param name="downloadRequests">List of files that would be downloaded</param>
        /// <returns>Analysis of potential bandwidth usage</returns>
        public static async Task<BandwidthAnalysis> AnalyzePotentialDownloadsAsync(List<DownloadRequest> downloadRequests)
        {
            var analysis = new BandwidthAnalysis();

            ApplicationWindow.WriteToDebugWindow($"🔍 Analyzing {downloadRequests.Count} potential downloads...\n");

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            foreach (var request in downloadRequests)
            {
                try
                {
                    bool fileExists = File.Exists(request.LocalFilePath);
                    long existingFileSize = 0;
                    long remoteFileSize = request.ExpectedSize;

                    if (fileExists)
                    {
                        existingFileSize = new FileInfo(request.LocalFilePath).Length;
                    }

                    if (remoteFileSize == 0)
                    {
                        remoteFileSize = await GetRemoteFileSizeQuickAsync(request.Url, httpClient);
                    }

                    bool wouldSkip = fileExists && existingFileSize == remoteFileSize && existingFileSize > 0;

                    if (wouldSkip)
                    {
                        analysis.FilesAlreadyDownloaded++;
                        analysis.BytesSaved += remoteFileSize;
                    }
                    else
                    {
                        analysis.FilesNeedingDownload++;
                        analysis.BytesToDownload += remoteFileSize;
                    }

                    analysis.TotalFiles++;
                    analysis.TotalBytes += remoteFileSize;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error analyzing download for {request.Url}: {ex.Message}");
                    analysis.FilesUnknown++;
                }
            }

            return analysis;
        }

        /// <summary>
        /// Quick method to get remote file size without full request
        /// </summary>
        /// <param name="url">URL to check</param>
        /// <param name="httpClient">HttpClient instance</param>
        /// <returns>File size or 0 if unknown</returns>
        private static async Task<long> GetRemoteFileSizeQuickAsync(string url, HttpClient httpClient)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                    return response.Content.Headers.ContentLength ?? 0;
                }
            }
            catch
            {

            }

            return 0;
        }

        /// <summary>
        /// Shows bandwidth analysis to user before starting downloads
        /// </summary>
        /// <param name="analysis">The bandwidth analysis results</param>
        public static void ShowBandwidthAnalysis(BandwidthAnalysis analysis)
        {
            if (analysis.TotalFiles == 0)
            {
                ApplicationWindow.WriteToDebugWindow("📊 No files to analyze\n");
                return;
            }

            ApplicationWindow.WriteToDebugWindow("📊 Bandwidth Analysis:\n");
            ApplicationWindow.WriteToDebugWindow($"   📁 Total files: {analysis.TotalFiles}\n");

            if (analysis.FilesAlreadyDownloaded > 0)
            {
                ApplicationWindow.WriteToDebugWindow($"   ✅ Already downloaded: {analysis.FilesAlreadyDownloaded} files ({FormatFileSize(analysis.BytesSaved)})\n");
            }

            if (analysis.FilesNeedingDownload > 0)
            {
                ApplicationWindow.WriteToDebugWindow($"   📥 Need to download: {analysis.FilesNeedingDownload} files ({FormatFileSize(analysis.BytesToDownload)})\n");
            }

            if (analysis.FilesUnknown > 0)
            {
                ApplicationWindow.WriteToDebugWindow($"   ❓ Unknown status: {analysis.FilesUnknown} files\n");
            }

            if (analysis.BytesSaved > 0)
            {
                double percentageSaved = (double)analysis.BytesSaved / analysis.TotalBytes * 100;
                ApplicationWindow.WriteToDebugWindow($"   💰 Bandwidth savings: {FormatFileSize(analysis.BytesSaved)} ({percentageSaved:F1}%)\n");
            }

            ApplicationWindow.WriteToDebugWindow("\n");
        }

        /// <summary>
        /// Example method showing how to use analysis before downloading
        /// </summary>
        /// <param name="channelName">Channel name</param>
        /// <param name="downloadRequests">Files to potentially download</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Download summary</returns>
        public static async Task<DownloadSummary> AnalyzeAndDownloadAsync(string channelName,
            List<DownloadRequest> downloadRequests, CancellationToken cancellationToken)
        {
            // First, analyze what we'd save
            var analysis = await AnalyzePotentialDownloadsAsync(downloadRequests);
            ShowBandwidthAnalysis(analysis);

            // Show user the potential savings
            if (analysis.BytesSaved > 0)
            {
                ApplicationWindow.WriteToDebugWindow($"🎉 Smart downloading will save {FormatFileSize(analysis.BytesSaved)} of bandwidth!\n");
            }

            // Then do the actual smart downloading
            using var httpClient = new HttpClient();
            return await SmartDownloadUtility.DownloadMultipleFilesAsync(downloadRequests, httpClient, cancellationToken);
        }

        /// <summary>
        /// Formats file size into human-readable format
        /// </summary>
        /// <param name="bytes">Size in bytes</param>
        /// <returns>Formatted size string</returns>
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
    }

    /// <summary>
    /// Analysis results for potential bandwidth usage
    /// </summary>
    public class BandwidthAnalysis
    {
        /// <summary>
        /// Total number of files analyzed
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Files that already exist and don't need downloading
        /// </summary>
        public int FilesAlreadyDownloaded { get; set; }

        /// <summary>
        /// Files that need to be downloaded
        /// </summary>
        public int FilesNeedingDownload { get; set; }

        /// <summary>
        /// Files with unknown status
        /// </summary>
        public int FilesUnknown { get; set; }

        /// <summary>
        /// Total bytes across all files
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Bytes that will be saved by not re-downloading existing files
        /// </summary>
        public long BytesSaved { get; set; }

        /// <summary>
        /// Bytes that actually need to be downloaded
        /// </summary>
        public long BytesToDownload { get; set; }

        /// <summary>
        /// Percentage of bandwidth that will be saved
        /// </summary>
        public double PercentageSaved => TotalBytes > 0 ? (double)BytesSaved / TotalBytes * 100 : 0;
    }
}
