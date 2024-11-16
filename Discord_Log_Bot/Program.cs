using Discord_Log_Bot;
using Microsoft.Extensions.Configuration;

class Program
{
    public static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsetting.json", optional: false, reloadOnChange: true);

        var configuration = builder.Build();

        var bot = new Bot(configuration);

        await bot.RunBotAsync();
    }
}
