using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord_Log_Bot.Controllers;
using Discord_Log_Bot.Enums;
using Discord_Log_Bot.LoggerModuls;
using System.Globalization;


namespace Discord_Log_Bot.Moduls
{
    public class CommandModule : ModuleBase<SocketCommandContext>
    {
        private readonly ChannelLogController _channelLogController;
        private readonly BotChannelController _botChannelController;

        public CommandModule(ChannelLogController channelLogController, BotChannelController botChannelController)
        {
            _channelLogController = channelLogController;
            _botChannelController = botChannelController;
        }

        #region Команды-помощника
        [Command("helplog")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task HelpCommand()
        {
            var embed = new EmbedBuilder()
                .WithTitle("Список команд")
                .WithDescription("Вот доступные команды:")
                .AddField("!helplog", "Показывает список доступных команд.")
                .AddField("!create_log_bot_channels", "Создает каналы для взаимодействия с ботом (желательно).")
                .AddField("!help_logcontoller", "Показывает список доступных команд по управлению логированием каналов")
                .AddField("!help_getlog", "Показывает список доступных команд для получения логов")
                .WithColor(Color.Blue)
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("help_logcontoller")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task HelpControllerLogCommand()
        {
            var embed = new EmbedBuilder()
                .WithTitle("Список команд")
                .WithDescription("Вот доступные команды с получениями логов пользователя:")
                .AddField("!enablelog <канал>", "Начинает логирование канала")
                .AddField("!disablelog <канал>", "Прекращает логирование канала")
                .AddField("!enablelogall", "Начинает логирование всех каналов")
                .AddField("!disablelogall", "Прекращает логирование всех каналов")
                .WithColor(Color.Green)
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("help_getlog")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task HelpUserLogCommand()
        {
            var embed = new EmbedBuilder()
                .WithTitle("Список команд")
                .WithDescription("Вот доступные команды с получениями логов:")
                .AddField("!getlogs <канал> <дата>", "Вы получити файл с логами по указаному каналу и дате")
                .WithColor(Color.Green)
                .Build();

            await ReplyAsync(embed: embed);
        }
        #endregion

        #region Команды по отслеживанию логов
        [Command("enablelog")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task EnableLogCommand(string channelArg)
        {
            await _channelLogController.EnableLogging(Context, channelArg);
        }

        [Command("disablelog")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DisableLogCommand(string channelArg)
        {
            await _channelLogController.DisableLogging(Context, channelArg);
        }


        [Command("enablelogall")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task EnableLoggingAllCommand()
        {
            await _channelLogController.EnableLoggingForAllChannels(Context);
        }

        [Command("disablelogall")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DisableLoggingForAllChannels()
        {
            await _channelLogController.DisableLoggingForAllChannels(Context);
        }
        #endregion

        [Command("getlogs")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task GetLogsCommand(string channelArg, string dateArg)
        {
            // Вызов метода из контроллера для получения логов
            await _channelLogController.GetLogsAsync(Context, channelArg, dateArg);
        }

        [Command("create_log_bot_channels")]
        [RequireUserPermission(GuildPermission.Administrator)] 
        public async Task StartLogBotCommand()
        {
           await _botChannelController.StartLogBotAsync(Context);
        }

    }
}
