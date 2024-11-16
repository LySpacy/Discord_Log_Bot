using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Discord_Log_Bot.LoggerModuls;
using Discord_Log_Bot.Moduls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Discord_Log_Bot.Controllers;

namespace Discord_Log_Bot
{
    public class Bot
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly CommandModule _commandModule;
        private readonly UserMessageLogController _userMessageLogController;
        private readonly ChannelLogController _channelLogController;
        private readonly IConfiguration _configuration;

        private HashSet<ulong> _loggingChannels = new HashSet<ulong>();

        public Bot(IConfiguration configuration)
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                GatewayIntents = GatewayIntents.Guilds
                                | GatewayIntents.GuildMessages
                                | GatewayIntents.GuildMembers
                                | GatewayIntents.MessageContent
            });
            _configuration = configuration;
            _commands = new CommandService();
            _userMessageLogController = new UserMessageLogController(_loggingChannels);
            _channelLogController = new ChannelLogController(_loggingChannels);

            var services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton(_loggingChannels)
                .AddSingleton(_channelLogController)
                .AddSingleton<BotChannelController>()
                .AddSingleton<CommandModule>()
                .BuildServiceProvider();

            _services = services;

            _commandModule = _services.GetRequiredService<CommandModule>();

            _client.Log += Log;
            _client.MessageReceived += HandleMessageAsync;
            _client.MessageUpdated += LogMessageUpdateAsync;
            _client.MessageDeleted += LogMessageDeleteAsync;
            _client.ChannelCreated += OnChannelCreated;
            _client.ThreadCreated += OnThreadCreated;
            _client.ThreadDeleted += OnThreadDeleted;
        }

        public async Task RunBotAsync()
        {
            string token = _configuration["DiscordBot:Token"];

            await ConfigureCommandsAsync();
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            Console.ReadLine();
        }
        public async Task ConfigureCommandsAsync()
        {
            await _commands.AddModulesAsync(System.Reflection.Assembly.GetEntryAssembly(), _services);
        }
        private Task Log(LogMessage logMessage)
        {
            Console.WriteLine(logMessage);
            return Task.CompletedTask;
        }

        private async Task HandleMessageAsync(SocketMessage message)
        {
            if (message is SocketUserMessage userMessage && !userMessage.Author.IsBot)
            {
                var context = new SocketCommandContext(_client, userMessage);

                int argPos = 0;

                if (userMessage.HasCharPrefix('!', ref argPos) || userMessage.HasMentionPrefix(_client.CurrentUser, ref argPos))
                {
                    var result = await _commands.ExecuteAsync(context, argPos, _services);

                    if (!result.IsSuccess)
                    {
                        Console.WriteLine($"Ошибка при выполнении команды: {result.ErrorReason}");
                    }
                }
                else if (_loggingChannels.Contains(userMessage.Channel.Id))
                {
                    await LogMessageAsync(userMessage);
                }
            }
        }
        private async Task OnChannelCreated(SocketChannel channel)
        {
            if (channel is ITextChannel textChannel)
            {
                await _channelLogController.StartLoggingForNewChannel((ITextChannel)channel);
            }
        }

        private async Task OnThreadCreated(SocketThreadChannel threadChannel)
        {
            await _channelLogController.OnThreadCreated(threadChannel);
        }

        private async Task OnThreadDeleted(Cacheable<SocketThreadChannel, ulong> threadChannelCache)
        {
            await _channelLogController.OnThreadDeleted(threadChannelCache);
        }
        private async Task LogMessageAsync(SocketMessage message)
        {
            await _userMessageLogController.LogMessageAsync(message);
        }
        private async Task LogMessageUpdateAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            await _userMessageLogController.LogMessageUpdateAsync(before, after, channel);
        }
        private async Task LogMessageDeleteAsync(Cacheable<IMessage, ulong> cacheableMessage, Cacheable<IMessageChannel, ulong> cacheableChannel)
        {
            await _userMessageLogController.LogMessageDeleteAsync(cacheableMessage, cacheableChannel);
        }
    }
}
