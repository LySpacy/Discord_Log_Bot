using Discord.WebSocket;
using Discord;
using Discord_Log_Bot.Models;
using Newtonsoft.Json;
using Discord_Log_Bot.Helpers;
using Discord_Log_Bot.Enums;
using System.Threading.Channels;

namespace Discord_Log_Bot.LoggerModuls
{
    public class UserMessageLogController
    {
        private readonly Dictionary<ulong, string> _messagesCache = new Dictionary<ulong, string>();
        private HashSet<ulong> _loggingChannels;

        public UserMessageLogController(HashSet<ulong> loggingChannels) => _loggingChannels = loggingChannels;

        public async Task LogMessageAsync(SocketMessage message)
        {
            if (message is not SocketUserMessage userMessage || userMessage.Author.IsBot)
                return; 

            // Сохраняем сообщение в кэш
            _messagesCache[message.Id] = message.Content;

            var log = new LogMessageModel
            {
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UserId= message.Author.Id,
                Action = ActionMessageType.Send,
                MessageContent = message.Content,
                Channel = (message.Channel as SocketTextChannel)?.Name ?? "Неизвестный канал",
                ChannelId = (message.Channel as SocketTextChannel)?.Id ?? default
            };

            string jsonLog = JsonConvert.SerializeObject(log, Formatting.Indented);
            await File.AppendAllTextAsync(FilePathHelper.GetLogFilePath(message.Channel), jsonLog + Environment.NewLine);
        }

        public async Task LogMessageUpdateAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            if (!_loggingChannels.Contains(channel.Id) || !(after is SocketUserMessage userMessage) || userMessage.Author.IsBot)
                return;  // Skip bots or channels not in the logging set

            if (_messagesCache.TryGetValue(after.Id, out string oldMessage))
            {
                string newMessage = userMessage.Content;
                var log = new LogMessageModel
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    UserId = after.Author.Id,
                    Action = ActionMessageType.Update,
                    MessageContent = $"Старое: {oldMessage} Новое: {newMessage}",
                    Channel = (channel as SocketTextChannel)?.Name ?? "Неизвестный канал",
                    ChannelId = (channel as SocketTextChannel)?.Id ?? default
                };

                string jsonLog = JsonConvert.SerializeObject(log, Formatting.Indented);
                await File.AppendAllTextAsync(FilePathHelper.GetLogFilePath(channel), jsonLog + Environment.NewLine);

                // Обновляем сообщение в кэше
                _messagesCache[after.Id] = newMessage;
            }
        }

        public async Task LogMessageDeleteAsync(Cacheable<IMessage, ulong> cacheableMessage, Cacheable<IMessageChannel, ulong> cacheableChannel)
        {
            var message = await cacheableMessage.GetOrDownloadAsync();
            var channel = await cacheableChannel.GetOrDownloadAsync();

            if (message is null || !_loggingChannels.Contains(channel?.Id ?? 0))
                return; 

            string logEntry;
            LogMessageModel log;

            if (_messagesCache.TryGetValue(message.Id, out string cachedMessageContent))
            {
                logEntry = $"[{DateTime.Now}] Сообщение было удалено в канале {channel?.Name}: {message?.Author?.Username}: {cachedMessageContent}";
                _messagesCache.Remove(message.Id);  // Удаляем сообщение из кэша

                log = new LogMessageModel
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    UserId = message?.Author?.Id ?? default,
                    Action = ActionMessageType.Delete,
                    MessageContent = cachedMessageContent,
                    Channel = (channel as SocketTextChannel)?.Name ?? "Неизвестный канал",
                    ChannelId = (channel as SocketTextChannel)?.Id ?? default
                };
            }
            else
            {
                logEntry = $"[{DateTime.Now}] Сообщение было удалено в канале {channel?.Name} (Сообщение недоступно в кеше)";
                log = new LogMessageModel
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    UserId = message?.Author?.Id ?? default,
                    Action = ActionMessageType.Delete,
                    MessageContent = "Сообщение недоступно в кеше",
                    Channel = (channel as SocketTextChannel)?.Name ?? "Неизвестный канал",
                    ChannelId = (channel as SocketTextChannel)?.Id ?? default
                };
            }

            string jsonLog = JsonConvert.SerializeObject(log, Formatting.Indented);
            await File.AppendAllTextAsync(FilePathHelper.GetLogFilePath((ISocketMessageChannel)channel), jsonLog + Environment.NewLine);
        }
    }
}
