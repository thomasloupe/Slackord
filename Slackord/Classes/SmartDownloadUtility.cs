namespace Slackord.Classes
{
    /// <summary>
    /// Utility for smart file downloading that avoids re-downloading existing complete files
    /// </summary>
    public static class SmartDownloadUtility
    {
        /// <summary>
        /// Downloads a file only if it doesn't exist or appears to be incomplete/corrupted
        /// </summary>
        /// <param name="url">The URL to download from</param>
        /// <param name="localFilePath">The local path where the file should be saved</param>
        /// <param name="expectedSize">Optional expected file size for validation (0 if unknown)</param>
        /// <param name="httpClient">HttpClient instance to use for downloading</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>DownloadResult indicating what happened</returns>
        public static async Task<DownloadResult> DownloadFileIfNeededAsync(string url, string localFilePath,
            long expectedSize = 0, HttpClient httpClient = null, CancellationToken cancellationToken = default)
        {
            try
            {
                string directory = Path.GetDirectoryName(localFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var existingFileStatus = CheckExistingFile(localFilePath, expectedSize);

                if (existingFileStatus == FileStatus.CompleteAndValid)
                {
                    return new DownloadResult
                    {
                        Status = DownloadStatus.Skipped,
                        Message = $"File already exists and appears complete: {Path.GetFileName(localFilePath)}",
                        LocalPath = localFilePath,
                        BytesDownloaded = 0
                    };
                }

                if (existingFileStatus == FileStatus.IncompleteOrCorrupted)
                {
                    ApplicationWindow.WriteToDebugWindow($"🔄 Re-downloading incomplete file: {Path.GetFileName(localFilePath)}\n");
                }

                bool disposeClient = false;
                if (httpClient == null)
                {
                    httpClient = new HttpClient();
                    disposeClient = true;
                }

                try
                {
                    if (expectedSize == 0)
                    {
                        expectedSize = await GetRemoteFileSizeAsync(url, httpClient, cancellationToken);
                    }

                    using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    long totalBytesToReceive = response.Content.Headers.ContentLength ?? expectedSize;
                    long totalBytesReceived = 0;

                    using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);

                    var buffer = new byte[8192];
                    int bytesReceived;

                    while ((bytesReceived = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesReceived, cancellationToken);
                        totalBytesReceived += bytesReceived;
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    bool downloadComplete = totalBytesToReceive == 0 || totalBytesReceived == totalBytesToReceive;

                    if (!downloadComplete && totalBytesToReceive > 0)
                    {
                        return new DownloadResult
                        {
                            Status = DownloadStatus.Failed,
                            Message = $"Download incomplete: {totalBytesReceived}/{totalBytesToReceive} bytes",
                            LocalPath = localFilePath,
                            BytesDownloaded = totalBytesReceived
                        };
                    }

                    return new DownloadResult
                    {
                        Status = DownloadStatus.Downloaded,
                        Message = $"Successfully downloaded: {Path.GetFileName(localFilePath)} ({FormatFileSize(totalBytesReceived)})",
                        LocalPath = localFilePath,
                        BytesDownloaded = totalBytesReceived
                    };
                }
                finally
                {
                    if (disposeClient)
                    {
                        httpClient.Dispose();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new DownloadResult
                {
                    Status = DownloadStatus.Failed,
                    Message = $"Download failed: {ex.Message}",
                    LocalPath = localFilePath,
                    BytesDownloaded = 0
                };
            }
        }

        /// <summary>
        /// Checks if an existing file is complete and valid
        /// </summary>
        /// <param name="filePath">Path to the file to check</param>
        /// <param name="expectedSize">Expected file size (0 if unknown)</param>
        /// <returns>Status of the existing file</returns>
        private static FileStatus CheckExistingFile(string filePath, long expectedSize)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return FileStatus.DoesNotExist;
                }

                var fileInfo = new FileInfo(filePath);

                if (fileInfo.Length == 0)
                {
                    return FileStatus.IncompleteOrCorrupted;
                }

                if (expectedSize > 0)
                {
                    if (fileInfo.Length == expectedSize)
                    {
                        return FileStatus.CompleteAndValid;
                    }
                    else if (fileInfo.Length < expectedSize)
                    {
                        return FileStatus.IncompleteOrCorrupted;
                    }
                }

                if (DateTime.Now - fileInfo.LastWriteTime < TimeSpan.FromMinutes(1))
                {
                    return FileStatus.IncompleteOrCorrupted;
                }

                return fileInfo.Length > 0 ? FileStatus.CompleteAndValid : FileStatus.IncompleteOrCorrupted;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error checking existing file {filePath}: {ex.Message}");
                return FileStatus.IncompleteOrCorrupted;
            }
        }

        /// <summary>
        /// Gets the remote file size without downloading the entire file
        /// </summary>
        /// <param name="url">URL to check</param>
        /// <param name="httpClient">HttpClient instance</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File size in bytes or 0 if unknown</returns>
        private static async Task<long> GetRemoteFileSizeAsync(string url, HttpClient httpClient, CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return response.Content.Headers.ContentLength ?? 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Could not get remote file size for {url}: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Downloads multiple files efficiently, showing progress
        /// </summary>
        /// <param name="downloads">List of download requests</param>
        /// <param name="httpClient">Shared HttpClient instance</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="progressCallback">Optional callback for progress updates</param>
        /// <returns>Summary of download results</returns>
        public static async Task<DownloadSummary> DownloadMultipleFilesAsync(
            List<DownloadRequest> downloads,
            HttpClient httpClient,
            CancellationToken cancellationToken,
            Action<int, int, string> progressCallback = null)
        {
            var summary = new DownloadSummary();

            ApplicationWindow.WriteToDebugWindow($"📥 Processing {downloads.Count} potential downloads...\n");

            for (int i = 0; i < downloads.Count; i++)
            {
                var download = downloads[i];
                cancellationToken.ThrowIfCancellationRequested();

                progressCallback?.Invoke(i + 1, downloads.Count, Path.GetFileName(download.LocalFilePath));

                var result = await DownloadFileIfNeededAsync(
                    download.Url,
                    download.LocalFilePath,
                    download.ExpectedSize,
                    httpClient,
                    cancellationToken);

                summary.AddResult(result);

                switch (result.Status)
                {
                    case DownloadStatus.Downloaded:
                        ApplicationWindow.WriteToDebugWindow($"✅ {result.Message}\n");
                        break;
                    case DownloadStatus.Skipped:
                        ApplicationWindow.WriteToDebugWindow($"⏭️ {result.Message}\n");
                        break;
                    case DownloadStatus.Failed:
                        ApplicationWindow.WriteToDebugWindow($"❌ {result.Message}\n");
                        break;
                }

                if (result.Status == DownloadStatus.Downloaded)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            ApplicationWindow.WriteToDebugWindow($"📊 Download Summary: {summary.GetSummaryText()}\n");
            return summary;
        }

        /// <summary>
        /// Formats file size into human-readable format
        /// </summary>
        /// <param name="bytes">Size in bytes</param>
        /// <returns>Formatted size string</returns>
        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
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
    /// Status of an existing file
    /// </summary>
    public enum FileStatus
    {
        /// <summary>
        /// File does not exist at the specified path
        /// </summary>
        DoesNotExist,

        /// <summary>
        /// File exists and is complete with valid content
        /// </summary>
        CompleteAndValid,

        /// <summary>
        /// File exists but is incomplete or corrupted
        /// </summary>
        IncompleteOrCorrupted
    }

    /// <summary>
    /// Result of a download operation
    /// </summary>
    public enum DownloadStatus
    {
        /// <summary>
        /// File was successfully downloaded
        /// </summary>
        Downloaded,

        /// <summary>
        /// File download was skipped because it already exists
        /// </summary>
        Skipped,

        /// <summary>
        /// File download failed
        /// </summary>
        Failed
    }

    /// <summary>
    /// Result of a single download operation
    /// </summary>
    public class DownloadResult
    {
        /// <summary>
        /// Status of the download operation
        /// </summary>
        public DownloadStatus Status { get; set; }

        /// <summary>
        /// Human-readable message about the result
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Local file path where the file was/would be saved
        /// </summary>
        public string LocalPath { get; set; }

        /// <summary>
        /// Number of bytes actually downloaded (0 for skipped files)
        /// </summary>
        public long BytesDownloaded { get; set; }
    }

    /// <summary>
    /// Request for downloading a file
    /// </summary>
    public class DownloadRequest
    {
        /// <summary>
        /// URL to download from
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Local path where file should be saved
        /// </summary>
        public string LocalFilePath { get; set; }

        /// <summary>
        /// Expected file size if known (0 if unknown)
        /// </summary>
        public long ExpectedSize { get; set; }

        /// <summary>
        /// Optional filename for display purposes
        /// </summary>
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// Summary of multiple download operations
    /// </summary>
    public class DownloadSummary
    {
        /// <summary>
        /// Number of files actually downloaded
        /// </summary>
        public int FilesDownloaded { get; private set; }

        /// <summary>
        /// Number of files skipped (already existed)
        /// </summary>
        public int FilesSkipped { get; private set; }

        /// <summary>
        /// Number of files that failed to download
        /// </summary>
        public int FilesFailed { get; private set; }

        /// <summary>
        /// Total bytes downloaded (not including skipped files)
        /// </summary>
        public long TotalBytesDownloaded { get; private set; }

        /// <summary>
        /// Total files processed
        /// </summary>
        public int TotalFiles => FilesDownloaded + FilesSkipped + FilesFailed;

        /// <summary>
        /// Adds a download result to the summary
        /// </summary>
        /// <param name="result">The download result to add</param>
        public void AddResult(DownloadResult result)
        {
            switch (result.Status)
            {
                case DownloadStatus.Downloaded:
                    FilesDownloaded++;
                    TotalBytesDownloaded += result.BytesDownloaded;
                    break;
                case DownloadStatus.Skipped:
                    FilesSkipped++;
                    break;
                case DownloadStatus.Failed:
                    FilesFailed++;
                    break;
            }
        }

        /// <summary>
        /// Gets a human-readable summary text
        /// </summary>
        /// <returns>Summary text</returns>
        public string GetSummaryText()
        {
            List<string> parts = [];

            if (FilesDownloaded > 0)
                parts.Add($"{FilesDownloaded} downloaded");

            if (FilesSkipped > 0)
                parts.Add($"{FilesSkipped} skipped");

            if (FilesFailed > 0)
                parts.Add($"{FilesFailed} failed");

            string summary = string.Join(", ", parts);

            if (TotalBytesDownloaded > 0)
            {
                summary += $" | {FormatFileSize(TotalBytesDownloaded)} downloaded";
            }

            return summary;
        }

        /// <summary>
        /// Formats file size into human-readable format
        /// </summary>
        /// <param name="bytes">Size in bytes</param>
        /// <returns>Formatted size string</returns>
        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
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
}
