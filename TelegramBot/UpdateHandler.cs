using Burger.Entity;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Burger.TelegramBot;

class TelegramMessage
{
    public required string Text { get; set; }
    public IReplyMarkup? ReplyMarkup { get; set; }
}


public class UpdateHandler(
    ILogger<UpdateHandler> logger,
    IServiceScopeFactory scopeFactory) : IUpdateHandler
{
    private TelegramMessage BuildMessageContent(Expense expense)
    {
        var text = expense.Deleted
            ? $"~💸\r\n*{expense.Category}*\n\r{expense.Amount} ₽~"
            : $"💸\r\n*{expense.Category}*\n\r{expense.Amount} ₽";

        var replyMarkup = expense.Deleted
            ? null
            : new InlineKeyboardMarkup([
                    InlineKeyboardButton.WithCallbackData("❌", "delete")
                ]);
        return new TelegramMessage {
            Text = text,
            ReplyMarkup = replyMarkup
        };
    }

    private static bool TryParseCategory(string message, out string category)
    {
        // Remove all numbers and some other symbols and get remaining string
        var excludeSymbols = new HashSet<char>(",.#");
        category = new string(message.Where(c => !char.IsDigit(c) && !excludeSymbols.Contains(c)).ToArray()).Trim();
        return !string.IsNullOrEmpty(category);
    }

    private static bool TryParseAmount(string message, out int amount)
    {
        var regex = new Regex("(\\d+)");
        var match = regex.Match(message);
        if (match.Success) {
            amount = int.Parse(match.Groups[1].Value);
            return true;
        }
        amount = 0;
        return false;
    }

    private async Task TryParseNewMessage(ITelegramBotClient botClient, Update update)
    {
        if (update.Message == null) return;

        var text = update.Message.Text;
        if (string.IsNullOrEmpty(text)) {
            return;
        }

        if (TryParseAmount(text, out var amount)) {
            if (!TryParseCategory(text, out var category)) {
                category = "Без категории";
            }
            var expense = new Expense {
                Amount = amount,
                Category = category,
                ChatId = update.Message.Chat.Id,
                Timestamp = update.Message.Date,
            };

            using (var scope = scopeFactory.CreateScope()) {
                var ctx = scope.ServiceProvider.GetRequiredService<BurgerContext>();
                var entyty = ctx.Expenses.Add(expense);
                ctx.SaveChanges();
            }
            var messageContent = BuildMessageContent(expense);
            var message = await botClient.SendTextMessageAsync(
                update.Message.Chat.Id,
                text: messageContent.Text,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2,
                replyMarkup: messageContent.ReplyMarkup
            );
            using (var scope = scopeFactory.CreateScope()) {
                var ctx = scope.ServiceProvider.GetRequiredService<BurgerContext>();
                var exp = ctx.Expenses.Find(expense.Id);
                if (exp != null) {
                    exp.MessageId = message.MessageId;
                    ctx.SaveChanges();
                }
            }
        }
    }

    private async Task TryParseCallback(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery == null) return;
        if (update.CallbackQuery.Message == null) return;

        var messageId = update.CallbackQuery.Message.MessageId;
        var data = update.CallbackQuery.Data;

        if (data == "delete") {
            using (var scope = scopeFactory.CreateScope()) {
                var ctx = scope.ServiceProvider.GetRequiredService<BurgerContext>();
                var expenseToRemove = ctx.Expenses.FirstOrDefault(x => x.MessageId == messageId);
                if (expenseToRemove != null) {
                    expenseToRemove.Deleted = true;
                    ctx.SaveChanges();

                    var messageContent = BuildMessageContent(expenseToRemove);

                    await botClient.EditMessageTextAsync(
                        update.CallbackQuery.Message.Chat.Id,
                        messageId,
                        messageContent.Text,
                        replyMarkup: messageContent.ReplyMarkup as InlineKeyboardMarkup,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2,
                        cancellationToken: cancellationToken);
                }
            }
        }
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try {
            await TryParseNewMessage(botClient, update);
            await TryParseCallback(botClient, update, cancellationToken);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error processing update");
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
