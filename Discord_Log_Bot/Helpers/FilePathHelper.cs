using Discord.WebSocket;

namespace Discord_Log_Bot.Helpers
{
    public static class FilePathHelper
    {
        public static string GetLogFilePath(ISocketMessageChannel channel)
        {
            var dateString = DateTime.UtcNow.ToString("yyyy-MM-dd");

            return $"{channel.Name}_{dateString}_logs.txt"; // Лог файл с именем канала
        }
    }
}
