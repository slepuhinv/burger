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
            ? $"<s>💸\r\n<b>{expense.Category}</b>\n\r{expense.Amount:C}</s>"
            : $"💸\r\n<b>{expense.Category}</b>\n\r{expense.Amount:C}";

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

            long[] allUsers = [];

            using (var scope = scopeFactory.CreateScope()) {
                var ctx = scope.ServiceProvider.GetRequiredService<BurgerContext>();
                var entyty = ctx.Expenses.Add(expense);
                ctx.SaveChanges();

                allUsers = ctx.Expenses.Select(x => x.ChatId).Where(x => x != update.Message.Chat.Id).Distinct().ToArray();
            }

            var messageContent = BuildMessageContent(expense);

            var message = await botClient.SendTextMessageAsync(
                    update.Message.Chat.Id,
                    text: messageContent.Text,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
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

            foreach (var userId in allUsers) {
                var userText =
                    $"{messageContent.Text}\r\n_[{update.Message.Chat.Username}](tg://user?id={update.Message.Chat.Id})_";

                await botClient.SendTextMessageAsync(
                    userId,
                    text: userText,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                    replyMarkup: messageContent.ReplyMarkup
                );
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
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                        cancellationToken: cancellationToken);
                }
            }
        }
    }

    private async Task TryHandleReport(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message?.Text == "/report") {
            using var scope = scopeFactory.CreateScope();
            var reportBuilder = scope.ServiceProvider.GetRequiredService<ReportBuilder>();
            var report = reportBuilder.GetReport();

            await botClient.SendTextMessageAsync(
                    update.Message.From.Id,
                    text: report
                );
        }
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try {
            await TryParseNewMessage(botClient, update);
            await TryParseCallback(botClient, update, cancellationToken);
            await TryHandleReport(botClient, update, cancellationToken );
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
