namespace Slackord.Classes
{
    using System;
    using System.IO;

    /// <summary>
    /// Provides logging functionality for the application with file-based output
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// The directory where log files are stored
        /// </summary>
        private static readonly string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        /// <summary>
        /// The filename for the current log file with timestamp
        /// </summary>
        private static readonly string logFileName = $"Slackord_{DateTime.Now:yyyyMMdd_HHmmss}.log";

        /// <summary>
        /// The full file path for the current log file
        /// </summary>
        private static readonly string logFilePath = Path.Combine(logsDirectory, logFileName);

        /// <summary>
        /// Static constructor that ensures the logs directory exists
        /// </summary>
        static Logger()
        {
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
        }

        /// <summary>
        /// Logs a message to the log file with timestamp
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log: {ex.Message}");
            }
        }
    }
}