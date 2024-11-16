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
            string botMessageChannelName = "log_bot-message";

            try
            {
                var logBotCategory = guild.CategoryChannels.FirstOrDefault(c => c.Name == "log_bot");
                if (logBotCategory == null)
                {
                    var createdCategory = await guild.CreateCategoryChannelAsync("log_bot"); // Создаём категорию

                    if (createdCategory != null)
                    {
                        // Повторно получаем категорию как SocketCategoryChannel
                        logBotCategory = guild.CategoryChannels.FirstOrDefault(c => c.Id == createdCategory.Id);

                        // Если по какой-то причине категория всё ещё null, выбрасываем исключение
                        if (logBotCategory == null)
                        {
                            throw new InvalidOperationException("Не удалось получить категорию log_bot после её создания.");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Создание категории log_bot завершилось неудачно.");
                    }


                    var everyoneRoleCategoryCreate = guild.EveryoneRole;
                    await logBotCategory.AddPermissionOverwriteAsync(everyoneRoleCategoryCreate, new OverwritePermissions(
                        viewChannel: PermValue.Deny
                    ));

                    var adminRoleCategoryCreate = guild.Roles.FirstOrDefault(r => r.Permissions.Administrator);
                    if (adminRoleCategoryCreate != null)
                    {
                        await logBotCategory.AddPermissionOverwriteAsync(adminRoleCategoryCreate, new OverwritePermissions(
                            viewChannel: PermValue.Allow,
                            manageChannel: PermValue.Allow
                        ));
                    }
                }
               

                var logsChannel = guild.TextChannels.FirstOrDefault(c => c.Name == logsChannelName);
                var commandChannel = guild.TextChannels.FirstOrDefault(c => c.Name == commandChannelName);
                var botMessageChannel = guild.TextChannels.FirstOrDefault(c => c.Name == botMessageChannelName);

                var adminRole = guild.Roles.FirstOrDefault(r => r.Permissions.Administrator);
                var everyoneRole = guild.EveryoneRole;

                if (logsChannel == null)
                {
                    var restLogsChannel = await guild.CreateTextChannelAsync(logsChannelName, x => x.CategoryId = logBotCategory.Id);

                    await restLogsChannel.AddPermissionOverwriteAsync(everyoneRole, new OverwritePermissions(
                        viewChannel: PermValue.Deny
                    ));

                    if (adminRole != null)
                    {
                        await restLogsChannel.AddPermissionOverwriteAsync(adminRole, new OverwritePermissions(
                            viewChannel: PermValue.Allow,
                            sendMessages: PermValue.Deny
                        ));
                    }

                    await contextCommand.Channel.SendMessageAsync($"Приватный канал для логов был создан: {restLogsChannel.Mention}");
                }
                else
                {
                    await contextCommand.Channel.SendMessageAsync($"Канал для логов уже существует: {logsChannel.Mention}");
                }

                if (commandChannel == null)
                {
                    var restCommandChannel = await guild.CreateTextChannelAsync(commandChannelName, x => x.CategoryId = logBotCategory.Id);

                    await restCommandChannel.AddPermissionOverwriteAsync(everyoneRole, new OverwritePermissions(
                        viewChannel: PermValue.Deny
                    ));

                    if (adminRole != null)
                    {
                        await restCommandChannel.AddPermissionOverwriteAsync(adminRole, new OverwritePermissions(
                            viewChannel: PermValue.Allow,
                            sendMessages: PermValue.Allow,
                            manageMessages: PermValue.Allow,
                            manageChannel: PermValue.Allow
                        ));
                    }

                    var embed = new EmbedBuilder()
                        .WithTitle("Вот что я умею")
                        .WithDescription("Вот доступные команды:")
                        .AddField("!helplog", "Показывает список доступных команд.")
                        .AddField("!create_log_bot_channels", "Создает каналы для взаимодействия с ботом (желательно).")
                        .AddField("!help_logcontoller", "Показывает список доступных команд по управлению логированием каналов.")
                        .AddField("!help_getlog", "Показывает список доступных команд для получения логов.")
                        .WithColor(Color.Blue)
                        .Build();

                    await restCommandChannel.SendMessageAsync("Вас приветствует бот для логирования текстовых каналов!");
                    await restCommandChannel.SendMessageAsync(embed: embed);
                    await contextCommand.Channel.SendMessageAsync($"Приватный канал для команд был создан: {restCommandChannel.Mention}");
                }
                else
                {
                    await contextCommand.Channel.SendMessageAsync($"Канал для команд уже существует: {commandChannel.Mention}");
                }

                if (botMessageChannel == null)
                {
                    var restBotMessageChannel = await guild.CreateTextChannelAsync(botMessageChannelName, x => x.CategoryId = logBotCategory.Id);

                    await restBotMessageChannel.AddPermissionOverwriteAsync(everyoneRole, new OverwritePermissions(
                        viewChannel: PermValue.Deny
                    ));

                    if (adminRole != null)
                    {
                        await restBotMessageChannel.AddPermissionOverwriteAsync(adminRole, new OverwritePermissions(
                            viewChannel: PermValue.Allow,
                            sendMessages: PermValue.Deny
                        ));
                    }

                    await contextCommand.Channel.SendMessageAsync($"Приватный канал для сообщений бота был создан: {restBotMessageChannel.Mention}");
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
