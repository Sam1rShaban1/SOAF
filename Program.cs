using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;
using MyApi.Repositories;
using MyApi.Services;

// Application entry point. Configures services, middleware, and the request pipeline.
var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------
// Service Registration
// -----------------------------------------------------------------------

// Add MVC controllers for handling HTTP requests.
builder.Services.AddControllers();

// Enable response caching middleware for [ResponseCache] attribute support.
builder.Services.AddResponseCaching();

// Add in-memory cache for service-layer caching (IMemoryCache).
builder.Services.AddMemoryCache();

// Register EF Core with SQLite using a factory pattern.
// IDbContextFactory ensures short-lived DbContext instances,
// avoiding concurrency issues with singleton-repositories.
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=MyApi.db"));

// Register repository and service as singletons.
// They use IDbContextFactory internally to create scoped DbContext instances.
builder.Services.AddSingleton<IProductRepository, ProductRepository>();
builder.Services.AddSingleton<IProductService, ProductService>();

// -----------------------------------------------------------------------
// Application Pipeline
// -----------------------------------------------------------------------

var app = builder.Build();

// Initialize the database: ensure schema exists and seed sample data on first run.
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = factory.CreateDbContext();

    // Create the database and tables if they do not exist.
    db.Database.EnsureCreated();

    // Seed 50 sample products (only the first 40 are active).
    if (!db.Products.Any())
    {
        db.Products.AddRange(
            Enumerable.Range(1, 50).Select(i => new Product
            {
                Id = i,
                Name = $"Product {i}",
                Price = i * 10m,
                IsActive = i <= 40      // Products 41-50 are inactive for testing soft-delete.
            }).ToArray()
        );
        db.SaveChanges();
    }
}

// Add response caching middleware to the pipeline (must be before MapControllers).
app.UseResponseCaching();

// Map controller routes so they handle incoming requests.
app.MapControllers();

// Start the web server and begin processing requests.
app.Run();
