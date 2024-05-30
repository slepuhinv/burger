using Burger.Entity;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace Burger.TelegramBot;

public class TelegramProcessor(
    IConfiguration configuration,
    IUpdateHandler updateHandler,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<TelegramProcessor> logger) : IHostedService
{
    private readonly string token = configuration.GetSection("TelegramBotToken").Get<string>() ?? throw new InvalidOperationException();

    private TelegramBotClient? botClient;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        botClient = new TelegramBotClient(token);
        await botClient.SetMyCommandsAsync([
            new BotCommand() { Command = "/edit_categories", Description = "Редактировать категории" },
            new BotCommand() { Command = "/report", Description = "Отчет" }
        ], cancellationToken: cancellationToken);


        botClient.StartReceiving(updateHandler, cancellationToken: cancellationToken);

        var releaseNotes = System.IO.File.ReadAllText("release.md");
        var startupText = $"Я запустился.\r\nv.{Assembly.GetExecutingAssembly().GetName().Version}\r\n{Environment.OSVersion}\r\nЧто нового:\r\n{releaseNotes}";

        using (var scope = serviceScopeFactory.CreateScope()) {
            var ctx = scope.ServiceProvider.GetRequiredService<BurgerContext>();
            var users = ctx.Expenses.Select(x => x.ChatId).Distinct().ToList();

            foreach (var user in users) {
                await botClient.SendTextMessageAsync(
                    user,
                    startupText,
                    cancellationToken: cancellationToken);
            }
        }

        logger.LogInformation("Bot started");
        //return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    //{
    //    var bot = new TelegramBotClient(token);


    //    bot.StartReceiving(async (client, update, ct) => {
    //        logger.LogInformation(update.Message?.Text);

    //        //var replyKeyboardMarkup = new ReplyKeyboardMarkup(new []
    //        //        {
    //        //            ["Help me", "Call me ☎️"],
    //        //            ["Some", "anothe rbutton",],
    //        //            ["products", "gtoceries"],
    //        //            ["products3", "gtoceries"],
    //        //            ["products4", "gtoceries"],
    //        //            new KeyboardButton[] {"products5", "gtoceries" },
    //        //        })
    //        //            {
    //        //                ResizeKeyboard = true, 
    //        //                OneTimeKeyboard = true
    //        //            };

    //        //await bot.SendTextMessageAsync(
    //        //    chatId: update.Message?.Chat.Id,
    //        //    text: "Choose a response",
    //        //    replyMarkup: replyKeyboardMarkup,
    //        //    cancellationToken: stoppingToken);

    //        //            InlineKeyboardMarkup inlineKeyboard = new(new[]
    //        //{
    //        //    // first row
    //        //    new []
    //        //    {
    //        //        InlineKeyboardButton.WithCallbackData(text: "1.1", callbackData: "11"),
    //        //        InlineKeyboardButton.WithCallbackData(text: "1.2", callbackData: "12"),
    //        //    },
    //        //    // second row
    //        //    new []
    //        //    {
    //        //        InlineKeyboardButton.WithCallbackData(text: "2.1", callbackData: "21"),
    //        //        InlineKeyboardButton.WithCallbackData(text: "2.2", callbackData: "22"),
    //        //    },
    //        //});

    //        //            Message sentMessage = await bot.SendTextMessageAsync(
    //        //                chatId: update.Message?.Chat.Id,
    //        //                text: "A message with an inline keyboard markup",
    //        //                replyMarkup: inlineKeyboard,
    //        //                cancellationToken: stoppingToken);

    //    }, (client, ex, ct) => {

    //    }, cancellationToken: stoppingToken);

    //    await taskCompletionSource.Task;
    //}

    //private ValueTask Bot_OnApiResponseReceived(Telegram.Bot.ITelegramBotClient botClient, Telegram.Bot.Args.ApiResponseEventArgs args, CancellationToken cancellationToken = default)
    //{

    //    return ValueTask.CompletedTask;
    //}
}
