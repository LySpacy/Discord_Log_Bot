using Discord.WebSocket;

namespace Discord_Log_Bot.Helpers
{
    public static class FilePathHelper
    {
        public static string GetLogFilePath(ISocketMessageChannel channel)
        {
            var logDirectory = Path.Combine("Logs", channel.Id.ToString());

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            var dateString = DateTime.UtcNow.ToString("yyyy-MM-dd");

            return $"{logDirectory}/{channel.Name}_{dateString}_logs.txt"; 
        }
    }
}
