using Discord_Log_Bot.Enums;
using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

namespace Discord_Log_Bot.Models
{
    public class LogLoggerEventModel
    {
        public string Timestamp { get; set; }
        public string Action { get; set; }
        public ulong UserId { get; set; }
        public string Channel { get; set; }
        public ulong ChannelId { get; set; }
    }
}
