using Burger.Entity;
using System.Text;

namespace Burger.TelegramBot;

public class ReportBuilder(BurgerContext burgerContext)
{
    public string GetReport()
    {
        var expenses = burgerContext
            .Expenses
            .Where(x => !x.Deleted)
            .AsEnumerable()
            .GroupBy(x => x.Category?.ToLower() ?? "Без категории")
            .Select(x => new { Category = $"{x.Key[0].ToString().ToUpper()}{x.Key[1..]}", Sum = x.Sum(e => e.Amount) })
            .OrderByDescending(x => x.Sum)
            .ToArray();

        var sb = new StringBuilder();
        var total = 0.0;

        sb.AppendLine("🤑 Все траты по категориям:");
        sb.AppendLine();

        foreach (var item in expenses) {
            sb.AppendLine($"{item.Category}: {item.Sum:C}");
            total += item.Sum;
        }

        sb.AppendLine();
        sb.AppendLine($"Итого: {total:C}");

        return sb.ToString();
    }
}
