using Discord_Log_Bot.Enums;

namespace Discord_Log_Bot.Models
{
    public class LogMessageModel
    {
        public string Timestamp { get; set; }
        public ActionMessageType Action { get; set; }
        public ulong UserId { get; set; }
        public string MessageContent { get; set; }
        public List<string> Attachments { get; set; } = new List<string>();
        public string Channel { get; set; }
        public ulong ChannelId { get; set; }
    }
}
