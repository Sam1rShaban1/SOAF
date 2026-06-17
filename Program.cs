using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;
using MyApi.Repositories;
using MyApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=MyApi.db"));

builder.Services.AddSingleton<IProductRepository, ProductRepository>();
builder.Services.AddSingleton<IProductService, ProductService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.EnsureCreated();

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            Enumerable.Range(1, 50).Select(i => new Product
            {
                Id = i,
                Name = $"Product {i}",
                Price = i * 10m,
                IsActive = i <= 40
            }).ToArray()
        );
        db.SaveChanges();
    }
}

app.UseResponseCaching();
app.MapControllers();

app.Run();
