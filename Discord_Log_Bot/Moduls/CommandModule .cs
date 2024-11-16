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

        // Команда !loghelp
        [Command("loghelp")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task HelpCommand()
        {
            var embed = new EmbedBuilder()
                .WithTitle("Список команд")
                .WithDescription("Вот доступные команды:")
                .AddField("!loghelp", "Показывает список доступных команд.")
                .AddField("!logcontollerhelp", "Показывает список доступных команд по управлению логированием каналов")
                .AddField("!helpsetnlog", "Показывает список доступных команд для получения логов")
                .WithColor(Color.Blue)
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("logcontollerhelp")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task HelpControllerLogCommand()
        {
            var embed = new EmbedBuilder()
                .WithTitle("Список команд")
                .WithDescription("Вот доступные команды с получениями логов пользователя:")
                .AddField("!enablelog <канал>", "Начинает логирование канала")
                .AddField("!disablelog <канал>", "Прекращает логирование канала")
                .WithColor(Color.Green)
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("helpsetnlog")]
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

        [Command("enablelog")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task EnableLogCommand(string channelArg)
        {
            await _channelLogController.EnableLogging(Context.Message, channelArg);
        }

        [Command("disablelog")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DisableLogCommand(string channelArg)
        {
            await _channelLogController.DisableLogging(Context.Message, channelArg);
        }

        [Command("getlogs")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task GetLogsCommand(string channelArg, string dateArg)
        {
            // Вызов метода из контроллера для получения логов
            await _channelLogController.GetLogsAsync(Context, channelArg, dateArg);
        }

        [Command("startlogbot")]
        [RequireUserPermission(GuildPermission.Administrator)] 
        public async Task StartLogBotCommand()
        {
           await _botChannelController.StartLogBotAsync(Context);
        }

        [Command("enablelogall")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task EnableLoggingAllCommand()
        {
            await _channelLogController.EnableLoggingForAllChannels(Context);
        }
    }
}
