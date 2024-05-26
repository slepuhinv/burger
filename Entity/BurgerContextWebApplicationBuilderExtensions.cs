namespace Burger.Entity;

public static class BurgerContextWebApplicationBuilderExtensions
{
    public static void AddBurgerContext(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<BurgerContextOptions>(builder.Configuration.GetSection("BurgerContext"));
        builder.Services.AddDbContext<BurgerContext>();
    }
}
