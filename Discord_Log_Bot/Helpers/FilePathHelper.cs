using Discord.WebSocket;

namespace Discord_Log_Bot.Helpers
{
    public static class FilePathHelper
    {
        public static string GetLogFilePath(ISocketMessageChannel channel)
        {
            string? logDirectory = null;
            if (channel is SocketThreadChannel)
            {
                logDirectory = Path.Combine("Logs/", $"Thread_{channel.Name}_{channel.Id}");
            }
            else
            {
                logDirectory = Path.Combine("Logs/", $"Channel_{channel.Name}_{channel.Id}");
            }

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            var dateString = DateTime.UtcNow.ToString("yyyy-MM-dd");

            return $"{logDirectory}/{channel.Name}_{dateString}_logs.txt"; 
        }
    }
}
