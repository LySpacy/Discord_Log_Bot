using Discord_Log_Bot.Enums;

namespace Discord_Log_Bot.Models
{
    public class LogLoggerEventModel
    {
        public string Timestamp { get; set; }
        public ActionEventType Action { get; set; }
        public ulong UserId { get; set; }
        public string Channel { get; set; }
        public ulong ChannelId { get; set; }
    }
}
