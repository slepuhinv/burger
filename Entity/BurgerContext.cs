using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Burger.Entity;

public class BurgerContext : DbContext
{
    private readonly BurgerContextOptions options;
    private readonly IHostEnvironment hostEnvironment;

    public DbSet<Expense> Expenses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var builder = optionsBuilder.UseSqlite($"Data Source={options.DbPath}");

        if (hostEnvironment.IsDevelopment()) {
            builder.LogTo(Console.WriteLine, LogLevel.Debug);
        }
    }

    public BurgerContext(IOptions<BurgerContextOptions> options, IHostEnvironment hostEnvironment)
    {
        this.options = options.Value;

        var directory = Path.GetDirectoryName(options.Value.DbPath);
        if (!string.IsNullOrEmpty(directory)) {
            Directory.CreateDirectory(directory);
        }

        this.hostEnvironment = hostEnvironment;
    }
}
