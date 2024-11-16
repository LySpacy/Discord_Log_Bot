using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord_Log_Bot.Enums;
using Discord_Log_Bot.Helpers;
using Discord_Log_Bot.Models;
using Newtonsoft.Json;
using System.Globalization;
using System.Reactive;
using System.Threading.Channels;

namespace Discord_Log_Bot.LoggerModuls
{
    public class ChannelLogController
    {
        private HashSet<ulong> _loggingChannels;

        public ChannelLogController(HashSet<ulong> loggingChannels) => _loggingChannels = loggingChannels;
        public async Task EnableLogging(SocketCommandContext contextCommand, string channelArg)
        {
            var message = contextCommand.Message;

            if (string.IsNullOrEmpty(channelArg))
            {
                await message.Channel.SendMessageAsync("Пожалуйста, укажите канал, например: `!enablelog #канал`");
                return;
            }

            string channelIdString = channelArg.Trim('<', '#', '>');
            SocketGuild guild = (message.Channel as SocketTextChannel)?.Guild;
            if (guild == null)
            {
                await message.Channel.SendMessageAsync("Не удалось найти сервер, на котором был вызван запрос.");
                return;
            }

            SocketTextChannel channel = guild.GetTextChannel(ulong.Parse(channelIdString));

            var logChannel = contextCommand.Guild.TextChannels.FirstOrDefault(c => c.Name == "log_bot-message");

            if (channel != null)
            {
                if (!_loggingChannels.Contains(channel.Id))
                {
                    _loggingChannels.Add(channel.Id);

                    // Создаем событие начала логирования
                    var logEvent = new LogLoggerEventModel
                    {
                        Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Action = ActionEventType.StartLogging.ToString(),
                        UserId = message.Author.Id,
                        Channel = channel.Name,
                        ChannelId = (channel as SocketTextChannel)?.Id ?? default
                    };

                    string jsonLog = JsonConvert.SerializeObject(logEvent, Formatting.Indented);
                    string logFilePath = FilePathHelper.GetLogFilePath(channel);
                    await Task.Run(() => File.AppendAllText(logFilePath, jsonLog + Environment.NewLine));

                    if (logChannel is not null)
                    {
                        await logChannel.SendMessageAsync($"<@{contextCommand.User.Id}> включил логирование для канала: <#{channelIdString}>");
                    }                
                }               
            }
        }

        public async Task DisableLogging(SocketCommandContext contextCommand, string channelArg)
        {
            var message = contextCommand.Message;

            if (string.IsNullOrEmpty(channelArg))
            {
                return;
            }

            SocketGuild guild = (message.Channel as SocketTextChannel)?.Guild;

            if (guild == null)
            {
                return;
            }

            string channelIdString = channelArg.Trim('<', '#', '>');

            SocketTextChannel channel = guild.GetTextChannel(ulong.Parse(channelIdString));

            var logChannel = contextCommand.Guild.TextChannels.FirstOrDefault(c => c.Name == "log_bot-message");

            if (channel != null)
            {
                if (_loggingChannels.Contains(channel.Id))
                {
                    _loggingChannels.Remove(channel.Id);

                    var logEvent = new LogLoggerEventModel
                    {
                        Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Action = ActionEventType.StopLogging.ToString(),
                        UserId = message.Author.Id,
                        Channel = channel.Name,
                        ChannelId = (channel as SocketTextChannel)?.Id ?? default
                    };

                    string jsonLog = JsonConvert.SerializeObject(logEvent, Formatting.Indented);
                    string logFilePath = FilePathHelper.GetLogFilePath(channel);

                    await File.AppendAllTextAsync(logFilePath, jsonLog + Environment.NewLine);

                    if (logChannel is not null)
                    {
                        await logChannel.SendMessageAsync($"<@{contextCommand.User.Id}> выключил логирование для канала: <#{channelIdString}>");
                    }
                }             
            }
           
        }

        public async Task GetLogsAsync(SocketCommandContext contextCommand, string channelArg, string dateArg)
        {
            DateTime parsedDate;
            var dateFormats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM-dd-yyyy", "yyyyMMdd","dd.MM.yyyy", "yyyy.MM.dd", "MM.dd.yyyy" };

            bool dateParsed = DateTime.TryParseExact(dateArg, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);

            if (!dateParsed)
            {
                await contextCommand.Channel.SendMessageAsync("Не удалось распарсить дату. Убедитесь, что дата указана в правильном формате (например, 2000-02-01 | 01.02.2000).");
                return;
            }

            var mentionedChannel = contextCommand.Message.MentionedChannels.FirstOrDefault();

            var channel = mentionedChannel ?? contextCommand.Guild.TextChannels.FirstOrDefault(c => c.Name.Equals(channelArg, StringComparison.OrdinalIgnoreCase));

            if (channel == null)
            {
                await contextCommand.Channel.SendMessageAsync("Не удалось найти канал. Убедитесь, что вы правильно указали канал.");
                return;
            }

            var dateString = parsedDate.ToString("yyyy-MM-dd");  // Дата в формате yyyy-MM-dd
            var logFilePath = FilePathHelper.GetLogFilePath(channel as ISocketMessageChannel);

            if (!File.Exists(logFilePath))
            {
                await contextCommand.Channel.SendMessageAsync("Лог-файл для указанного канала и даты не найден.");
                return;
            }

            var logChannel = contextCommand.Guild.TextChannels.FirstOrDefault(c => c.Name == "log_bot-logs");

            if (logChannel == null)
            {
                await contextCommand.Channel.SendMessageAsync("Не удалось найти канал для логирования: log_bot-logs.");
                return;
            }

            try
            {
                using (var fileStream = new FileStream(logFilePath, FileMode.Open))
                {
                    await logChannel.SendFileAsync(fileStream, Path.GetFileName(logFilePath), "Вот лог-файл для указанного канала и даты.");
                }

                await contextCommand.Channel.SendMessageAsync($"Лог-файл для канала {channel.Name} успешно отправлен в канал <#{logChannel.Id}>.");
            }
            catch (Exception ex)
            {
                await contextCommand.Channel.SendMessageAsync($"Произошла ошибка при отправке файла: {ex.Message}");
            }
        }

        public async Task StartLoggingForNewChannel(ITextChannel channel)
        {
            if (!_loggingChannels.Contains(channel.Id))
            {
                _loggingChannels.Add(channel.Id);

                var logEvent = new LogLoggerEventModel
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Action = ActionEventType.StartLogging.ToString(),
                    UserId = default,  
                    Channel = channel.Name,
                    ChannelId = channel.Id
                };

                string jsonLog = JsonConvert.SerializeObject(logEvent, Formatting.Indented);
                string logFilePath = FilePathHelper.GetLogFilePath(channel as ISocketMessageChannel);

                await Task.Run(() => File.AppendAllText(logFilePath, jsonLog + Environment.NewLine));
            }
        }

        public async Task EnableLoggingForAllChannels(SocketCommandContext context)
        {
            var guild = context.Guild;
            if (guild == null)
            {
                await context.Channel.SendMessageAsync("Не удалось найти сервер.");
                return;
            }

            var excludedChannelNames = new HashSet<string>
            {
                "log_bot-logs",
                "log_bot-command",
                "log_bot-message"
            };

            var logChannel = context.Guild.TextChannels.FirstOrDefault(c => c.Name == "log_bot-message");

            if (logChannel is not null)
            {
                await context.Channel.SendMessageAsync($"<@{context.User.Id}> Решил включить логирование для всех текстовых каналов...");
                await logChannel.SendMessageAsync($"<@{context.User.Id}> начал включение логирования для всех каналов...");

                foreach (var channel in guild.TextChannels)
                {
                    if (excludedChannelNames.Contains(channel.Name))
                    {
                        continue;
                    }

                    await StartLoggingForNewChannel(channel);
                    await logChannel.SendMessageAsync($"Логирование включено для канала <#{channel.Id}>");
                }

                await context.Channel.SendMessageAsync("Логирование включено для всех текстовых каналов.");
                await logChannel.SendMessageAsync("Логирование включено для всех текстовых каналов.");
            }
            else
            {
                context.Channel.SendMessageAsync("Не удалось найти канал для логирования: log_bot-message");
            }
        }
        public async Task DisableLoggingForAllChannels(SocketCommandContext context)
        {
            var guild = context.Guild;
            if (guild == null)
            {
                await context.Channel.SendMessageAsync("Не удалось найти сервер.");
                return;
            }

            var excludedChannelNames = new HashSet<string>
            {
                "log_bot-logs",
                "log_bot-command",
                "log_bot-message"
            };

            var logChannel = context.Guild.TextChannels.FirstOrDefault(c => c.Name == "log_bot-message");

            if (logChannel is not null)
            {
                bool anyDisabled = false;

                await context.Channel.SendMessageAsync($"<@{context.User.Id}> начал отключения логирования для канала всех каналов...");
                await logChannel.SendMessageAsync($"<@{context.User.Id}> начал отключения логирования для канала всех каналов...");

                foreach (var channel in guild.TextChannels)
                {

                    if (excludedChannelNames.Contains(channel.Name) || channel is SocketVoiceChannel)
                    {
                        continue;
                    }

                    if (_loggingChannels.Contains(channel.Id))
                    {
                        await StopLoggingForChannel(context, channel);

                        anyDisabled = true;

                        await logChannel.SendMessageAsync($"Логирование отключено для канала <#{channel.Id}>");
                    }
                }

                if (!anyDisabled)
                {
                    await context.Channel.SendMessageAsync("Логирование не было включено в каналах.");
                    await logChannel.SendMessageAsync("Логирование не было включено в каналах.");
                }
                else
                {
                    await context.Channel.SendMessageAsync("Логирование отключено для всех каналов.");
                    await logChannel.SendMessageAsync("Логирование отключено для всех каналов.");
                }
            }
            else
            {
                context.Channel.SendMessageAsync("Не удалось найти канал для логирования: log_bot-message");
            }
        }
        public async Task OnThreadCreated(SocketThreadChannel threadChannel)
        {
            if (!_loggingChannels.Contains(threadChannel.Id))
            {
                _loggingChannels.Add(threadChannel.Id);

                var log = new LogMessageModel
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    UserId = threadChannel.Owner.Id,
                    Action = "Create tread",
                    MessageContent = $"Создана ветка с названием {threadChannel.Name}, ID: {threadChannel.Id}",
                    Channel = threadChannel.ParentChannel.Name,
                    ChannelId = threadChannel.ParentChannel.Id
                };

                var parent = threadChannel.ParentChannel;
                var parentLog = new LogMessageModel()
                {  
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    UserId = threadChannel.Owner.Id,
                    Action = "Create tread",
                    MessageContent = $"Создана ветка с названием {threadChannel.Name}, ID: {threadChannel.Id}",
                    Channel = parent.Name,
                    ChannelId = parent.Id          
                };

                string jsonLogThread = JsonConvert.SerializeObject(log, Formatting.Indented);
                string jsonLogParent = JsonConvert.SerializeObject(log, Formatting.Indented);

                var pathLogThread = FilePathHelper.GetLogFilePath(threadChannel);
                var pathLogParent = FilePathHelper.GetLogFilePath(parent as ISocketMessageChannel);

                await File.AppendAllTextAsync(pathLogThread, jsonLogThread + Environment.NewLine);
                await File.AppendAllTextAsync(pathLogParent, jsonLogParent + Environment.NewLine);
            }
        }

        public async Task OnThreadDeleted(Cacheable<SocketThreadChannel, ulong> threadChannelCache)
        {
            var threadChannel = threadChannelCache.HasValue ? threadChannelCache.Value : null;

            if (threadChannel == null)
            {
                return;
            }

            if (_loggingChannels.Contains(threadChannel.Id))
            {
                _loggingChannels.Remove(threadChannel.Id);

                var log = new LogMessageModel
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    UserId = threadChannel.CurrentUser?.Id ?? 0,
                    Action = ActionMessageType.Delete.ToString(),
                    MessageContent = $"Ветка с названием {threadChannel.Name} была удалена, ID: {threadChannel.Id}",
                    Channel = threadChannel.ParentChannel.Name,
                    ChannelId = threadChannel.ParentChannel.Id
                };

                var parent = threadChannel.ParentChannel;
                var parentLog = new LogMessageModel()
                {
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    UserId = threadChannel.CurrentUser?.Id ?? 0,
                    Action = ActionMessageType.Delete.ToString(),
                    MessageContent = $"Ветка с названием {threadChannel.Name} была удалена, ID: {threadChannel.Id}",
                    Channel = parent.Name,
                    ChannelId = parent.Id
                };

                string jsonLogThread = JsonConvert.SerializeObject(log, Formatting.Indented);
                string jsonLogParent = JsonConvert.SerializeObject(log, Formatting.Indented);

                var pathLogThread = FilePathHelper.GetLogFilePath(threadChannel);
                var pathLogParent = FilePathHelper.GetLogFilePath(parent as ISocketMessageChannel);

                await File.AppendAllTextAsync(pathLogThread, jsonLogThread + Environment.NewLine);
                await File.AppendAllTextAsync(pathLogParent, jsonLogParent + Environment.NewLine);
            }
        }
        private async Task StopLoggingForChannel(SocketCommandContext context, SocketTextChannel channel)
        {
            if (channel != null)
            {
                if (_loggingChannels.Contains(channel.Id))
                {
                    _loggingChannels.Remove(channel.Id);

                    var logEvent = new LogLoggerEventModel
                    {
                        Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Action = ActionEventType.StopLogging.ToString(),
                        UserId = context.User.Id,
                        Channel = channel.Name,
                        ChannelId = (channel as SocketTextChannel)?.Id ?? default
                    };

                    string jsonLog = JsonConvert.SerializeObject(logEvent, Formatting.Indented);

                    string logFilePath = FilePathHelper.GetLogFilePath(channel);
                    await File.AppendAllTextAsync(logFilePath, jsonLog + Environment.NewLine);
                }
            }

            await Task.CompletedTask;
        }
    }
}
