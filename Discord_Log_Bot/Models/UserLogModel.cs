using Discord_Log_Bot.Enums;


namespace Discord_Log_Bot.Models
{
    public class UserLogModel
    {
        public ulong UserId { get; set; }
        public ActionMessageType Type { get; set; }
        public string Content { get; set; }
        public string ChannelName { get; set; }
        public ulong ChannelId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
