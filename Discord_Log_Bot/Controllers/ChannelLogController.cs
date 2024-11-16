using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord_Log_Bot.Enums;
using Discord_Log_Bot.Helpers;
using Discord_Log_Bot.Models;
using Newtonsoft.Json;
using System.Globalization;

namespace Discord_Log_Bot.LoggerModuls
{
    public class ChannelLogController
    {
        private HashSet<ulong> _loggingChannels;

        public ChannelLogController(HashSet<ulong> loggingChannels) => _loggingChannels = loggingChannels;
        public async Task EnableLogging(SocketUserMessage message, string channelArg)
        {
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

            if (channel != null)
            {
                if (!_loggingChannels.Contains(channel.Id))
                {
                    _loggingChannels.Add(channel.Id);

                    // Создаем событие начала логирования
                    var logEvent = new LogLoggerEventModel
                    {
                        Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Action = ActionEventType.StartLogging,
                        UserId = message.Author.Id,
                        Channel = channel.Name,
                        ChannelId = (channel as SocketTextChannel)?.Id ?? default
                    };

                    string jsonLog = JsonConvert.SerializeObject(logEvent, Formatting.Indented);
                    string logFilePath = FilePathHelper.GetLogFilePath(channel);
                    await Task.Run(() => File.AppendAllText(logFilePath, jsonLog + Environment.NewLine));

                    await message.Channel.SendMessageAsync($"Логирование включено для канала: <#{channelIdString}>");
                }               
            }            
        }

        public async Task DisableLogging(SocketUserMessage message, string channelArg)
        {
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

            if (channel != null)
            {
                if (_loggingChannels.Contains(channel.Id))
                {
                    _loggingChannels.Remove(channel.Id);

                    var logEvent = new LogLoggerEventModel
                    {
                        Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Action = ActionEventType.StopLogging,
                        UserId = message.Author.Id,
                        Channel = channel.Name,
                        ChannelId = (channel as SocketTextChannel)?.Id ?? default
                    };

                    string jsonLog = JsonConvert.SerializeObject(logEvent, Formatting.Indented);

                    string logFilePath = FilePathHelper.GetLogFilePath(channel);
                    await File.AppendAllTextAsync(logFilePath, jsonLog + Environment.NewLine);
                }             
            }
           
        }

        public async Task GetLogsAsync(SocketCommandContext contexCommand, string channelArg, string dateArg)
        {
            var pathToLog = "";

            DateTime parsedDate;
            var dateFormats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "MM-dd-yyyy", "yyyyMMdd","dd.MM.yyyy", "yyyy.MM.dd", "MM.dd.yyyy" };

            bool dateParsed = DateTime.TryParseExact(dateArg, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);

            if (!dateParsed)
            {
                await contexCommand.Channel.SendMessageAsync("Не удалось распарсить дату. Убедитесь, что дата указана в правильном формате (например, 2000-02-01 | 01.02.2000).");
                return;
            }

            var mentionedChannel = contexCommand.Message.MentionedChannels.FirstOrDefault();

            var channel = mentionedChannel ?? contexCommand.Guild.TextChannels.FirstOrDefault(c => c.Name.Equals(channelArg, StringComparison.OrdinalIgnoreCase));

            if (channel == null)
            {
                await contexCommand.Channel.SendMessageAsync("Не удалось найти канал. Убедитесь, что вы правильно указали канал.");
                return;
            }

            var dateString = parsedDate.ToString("yyyy-MM-dd");  // Дата в формате yyyy-MM-dd
            var logFilePath = Path.Combine($"{pathToLog}", $"{channel.Name}_{dateString}_logs.txt");

            if (!File.Exists(logFilePath))
            {
                await contexCommand.Channel.SendMessageAsync("Лог-файл для указанного канала и даты не найден.");
                return;
            }

            if (channel is ITextChannel textChannel)
            {
                try
                {
                    var fileStream = new FileStream(logFilePath, FileMode.Open);
                    await contexCommand.Channel.SendFileAsync(fileStream, $"{channel.Name}_{dateString}_logs.txt", "Вот лог-файл для указанного канала и даты.");
                    fileStream.Close();
                }
                catch (Exception ex)
                {
                    await contexCommand.Channel.SendMessageAsync($"Произошла ошибка при отправке файла: {ex.Message}");
                }
            }
            else
            {
                await contexCommand.Channel.SendMessageAsync("Указанный канал не является текстовым каналом.");
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
                    Action = ActionEventType.StartLogging,
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
                "log-bot_message"
            };

            await context.Channel.SendMessageAsync("Начинаю включать логирование для всех текстовых каналов...");

            foreach (var channel in guild.TextChannels)
            {
                if (excludedChannelNames.Contains(channel.Name))
                {
                    continue; 
                }

                await StartLoggingForNewChannel(channel);
                await context.Channel.SendMessageAsync($"Логирование включено для канала <#{channel.Id}>");
            }

            await context.Channel.SendMessageAsync("Логирование включено для всех текстовых каналов.");
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
                "log-bot_message"
            };

            bool anyDisabled = false;

       
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

                    await context.Channel.SendMessageAsync($"Логирование отключено для канала <#{channel.Id}>");
                }
            }

            if (!anyDisabled)
            {
                await context.Channel.SendMessageAsync("Логирование не было включено в каналах.");
            }
            else
            {
                await context.Channel.SendMessageAsync("Логирование отключено для всех канала.");
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
                        Action = ActionEventType.StopLogging,
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
