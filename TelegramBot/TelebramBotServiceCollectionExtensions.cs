using Telegram.Bot.Polling;

namespace Burger.TelegramBot;

public static class TelebramBotServiceCollectionExtensions
{
    public static void AddTelegramBot(this IServiceCollection services)
    {
        services.AddSingleton<IUpdateHandler, UpdateHandler>();
        services.AddSingleton<TelegramProcessor>();
        services.AddHostedService(x => x.GetRequiredService<TelegramProcessor>());
        services.AddTransient<ReportBuilder>();
    }
}
