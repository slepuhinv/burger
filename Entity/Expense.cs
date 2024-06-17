namespace Burger.Entity;

public class Expense
{
    public long Id { get; set; }
    public long ChatId { get; set; }
    public int MessageId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Amount { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool Deleted { get; set; }
}
