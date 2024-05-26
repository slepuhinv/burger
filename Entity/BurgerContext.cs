using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Burger.Entity;

public class BurgerContext : DbContext
{
    private BurgerContextOptions options;

    public DbSet<Expense> Expenses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={options.DbPath}");
    }

    public BurgerContext(IOptions<BurgerContextOptions> options)
    {
        this.options = options.Value;

        Directory.CreateDirectory(Path.GetDirectoryName(options.Value.DbPath)!);
    }
}
