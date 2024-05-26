using Burger.Entity;
using Burger.TelegramBot;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddBurgerContext();
builder.Services.AddTelegramBot();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/expense", (BurgerContext dbContext) => {
    return dbContext.Expenses;
});

app.MapPost("/expense", (BurgerContext dbContext) => {
    dbContext.Expenses.Add(new Expense { Amount = 123, Timestamp = DateTime.UtcNow, Category = "new" });
    dbContext.SaveChanges();
});

using (var scope = app.Services.CreateScope()) {
    var ctx = scope.ServiceProvider.GetRequiredService<BurgerContext>();
    ctx.Database.Migrate();
}

app.Run();
