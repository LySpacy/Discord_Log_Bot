using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;


namespace Discord_Log_Bot.Controllers
{
    public class BotChannelController
    {
        public async Task StartLogBotAsync(SocketCommandContext contextCommand)
        {
            var guild = (contextCommand.Channel as SocketTextChannel)?.Guild;

            if (guild == null)
            {
                await contextCommand.Channel.SendMessageAsync("Не удалось найти сервер.");
                return;
            }

            string logsChannelName = "log_bot-logs";
            string commandChannelName = "log_bot-command";
            string botMessageChannelName = "log-bot_message";

            try
            {
                var logsChannel = guild.TextChannels.FirstOrDefault(c => c.Name == logsChannelName);
                var commandChannel = guild.TextChannels.FirstOrDefault(c => c.Name == commandChannelName);
                var botMessageChannel = guild.TextChannels.FirstOrDefault(c => c.Name == botMessageChannelName);

                if (logsChannel == null)
                {
                    var restLogsChannel = await guild.CreateTextChannelAsync(logsChannelName);
                    
                    //await UpdateLogChannelIdAsync(logsChannelName, restLogsChannel.Id);
                    await contextCommand.Channel.SendMessageAsync($"Канал для логов был создан: {restLogsChannel.Mention}");
                }
                else
                {
                    await contextCommand.Channel.SendMessageAsync($"Канал для логов уже существует: {logsChannel.Mention}");
                }

                if (commandChannel == null)
                {
                    var restCommandChannel = await guild.CreateTextChannelAsync(commandChannelName);

                    //await UpdateLogChannelIdAsync(commandChannelName, restCommandChannel.Id);
                    await contextCommand.Channel.SendMessageAsync($"Канал для команд был создан: {restCommandChannel.Mention}");
                }
                else
                {
                    await contextCommand.Channel.SendMessageAsync($"Канал для команд уже существует: {commandChannel.Mention}");
                }

                if (botMessageChannel == null)
                {
                    var restBotMessageChannel = await guild.CreateTextChannelAsync(botMessageChannelName);

                    //await UpdateLogChannelIdAsync(botMessageChannelName, restBotMessageChannel.Id);
                    await contextCommand.Channel.SendMessageAsync($"Канал для сообщений бота был создан: {restBotMessageChannel.Mention}");
                }
                else
                {
                    await contextCommand.Channel.SendMessageAsync($"Канал для сообщений бота уже существует: {botMessageChannel.Mention}");
                }
            }
            catch (Exception ex)
            {
                await contextCommand.Channel.SendMessageAsync($"Произошла ошибка при создании каналов: {ex.Message}");
            }
        }

        //public async Task UpdateLogChannelIdAsync(string channelKey, ulong newChannelId)
        //{
        //    const string filePath = "channels.json";

        //    try
        //    {
        //        Dictionary<string, ulong> serverChannels;

        //        if (File.Exists(filePath))
        //        {
        //            var channels = await File.ReadAllTextAsync(filePath);
        //            serverChannels = JsonConvert.DeserializeObject<Dictionary<string, ulong>>(channels);
        //        }
        //        else
        //        {
        //            serverChannels = new Dictionary<string, ulong>
        //            {       
        //                { "log_bot-logs", 0 },  
        //                { "log_bot-command", 0 }, 
        //                { "log_bot_messages", 0 } 
        //            };
        //        }

        //        if (serverChannels.ContainsKey(channelKey))
        //        {
        //            serverChannels[channelKey] = newChannelId; 
        //        }
        //        else
        //        {
        //            serverChannels.Add(channelKey, newChannelId); 
        //        }

        //        await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(serverChannels, Formatting.Indented));
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}      
    }
}
