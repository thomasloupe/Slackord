namespace Slackord.Classes
{
    using System;
    using System.IO;

    public class Logger
    {
        private static readonly string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private static readonly string logFileName = $"Slackord_{DateTime.Now:yyyyMMdd_HHmmss}.log";
        private static readonly string logFilePath = Path.Combine(logsDirectory, logFileName);

        static Logger()
        {
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
        }

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
