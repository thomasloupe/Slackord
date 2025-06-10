using Newtonsoft.Json;
using System.Text;

namespace Slackord.Classes
{
    /// <summary>
    /// Manages reading and writing .slackord files containing converted message data
    /// </summary>
    public static class SlackordFileManager
    {
        /// <summary>
        /// Saves reconstructed messages to a .slackord file
        /// </summary>
        /// <param name="filePath">The path where the file should be saved</param>
        /// <param name="messages">The list of reconstructed messages to save</param>
        public static async Task SaveChannelMessagesAsync(string filePath, List<ReconstructedMessage> messages)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                Directory.CreateDirectory(directory);

                string json = JsonConvert.SerializeObject(messages, Formatting.Indented);

                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"❌ Error saving channel file {filePath}: {ex.Message}\n");
                Logger.Log($"SlackordFileManager.SaveChannelMessagesAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads reconstructed messages from a .slackord file
        /// </summary>
        /// <param name="filePath">The path to the .slackord file to load</param>
        /// <returns>A list of reconstructed messages, or empty list if file doesn't exist</returns>
        public static async Task<List<ReconstructedMessage>> LoadChannelMessagesAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    ApplicationWindow.WriteToDebugWindow($"❌ Channel file not found: {filePath}\n");
                    return [];
                }

                string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                var messages = JsonConvert.DeserializeObject<List<ReconstructedMessage>>(json) ?? [];

                ApplicationWindow.WriteToDebugWindow($"📂 Loaded {messages.Count:N0} messages from {Path.GetFileName(filePath)}\n");
                return messages;
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"❌ Error loading channel file {filePath}: {ex.Message}\n");
                Logger.Log($"SlackordFileManager.LoadChannelMessagesAsync: {ex.Message}");
                return [];
            }
        }

        /// <summary>
        /// Appends a single message to an existing .slackord file (for incremental saves)
        /// </summary>
        /// <param name="filePath">The path to the .slackord file</param>
        /// <param name="message">The message to append</param>
        public static async Task AppendMessageAsync(string filePath, ReconstructedMessage message)
        {
            try
            {
                var messages = await LoadChannelMessagesAsync(filePath);
                messages.Add(message);
                await SaveChannelMessagesAsync(filePath, messages);
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"❌ Error appending to channel file {filePath}: {ex.Message}\n");
                Logger.Log($"SlackordFileManager.AppendMessageAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the number of messages in a .slackord file without loading all data
        /// </summary>
        /// <param name="filePath">The path to the .slackord file</param>
        /// <returns>The number of messages in the file, or 0 if file doesn't exist</returns>
        public static async Task<int> GetMessageCountAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return 0;

                var messages = await LoadChannelMessagesAsync(filePath);
                return messages.Count;
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"❌ Error getting message count from {filePath}: {ex.Message}\n");
                Logger.Log($"SlackordFileManager.GetMessageCountAsync: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Loads a range of messages from a .slackord file (for pagination/resume)
        /// </summary>
        /// <param name="filePath">The path to the .slackord file</param>
        /// <param name="startIndex">The starting index of messages to load</param>
        /// <param name="count">The number of messages to load</param>
        /// <returns>A list of messages in the specified range</returns>
        public static async Task<List<ReconstructedMessage>> LoadMessageRangeAsync(string filePath, int startIndex, int count)
        {
            try
            {
                var allMessages = await LoadChannelMessagesAsync(filePath);

                if (startIndex >= allMessages.Count)
                    return [];

                int actualCount = Math.Min(count, allMessages.Count - startIndex);
                return [.. allMessages.Skip(startIndex).Take(actualCount)];
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"❌ Error loading message range from {filePath}: {ex.Message}\n");
                Logger.Log($"SlackordFileManager.LoadMessageRangeAsync: {ex.Message}");
                return [];
            }
        }

        /// <summary>
        /// Deletes a channel's .slackord file
        /// </summary>
        /// <param name="filePath">The path to the .slackord file to delete</param>
        public static void DeleteChannelFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    ApplicationWindow.WriteToDebugWindow($"🗑️ Deleted channel file: {Path.GetFileName(filePath)}\n");
                }
            }
            catch (Exception ex)
            {
                ApplicationWindow.WriteToDebugWindow($"❌ Error deleting channel file {filePath}: {ex.Message}\n");
                Logger.Log($"SlackordFileManager.DeleteChannelFile: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates that a .slackord file contains valid data
        /// </summary>
        /// <param name="filePath">The path to the .slackord file to validate</param>
        /// <returns>True if the file exists and contains valid message data</returns>
        public static async Task<bool> ValidateChannelFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var messages = await LoadChannelMessagesAsync(filePath);
                return messages != null && messages.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets file size in a human-readable format
        /// </summary>
        /// <param name="filePath">The path to the file</param>
        /// <returns>A formatted string showing the file size (e.g., "1.5 MB")</returns>
        public static string GetFileSizeDisplay(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return "0 B";

                var fileInfo = new FileInfo(filePath);
                long bytes = fileInfo.Length;

                string[] sizes = ["B", "KB", "MB", "GB"];
                double len = bytes;
                int order = 0;

                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }

                return $"{len:0.##} {sizes[order]}";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}