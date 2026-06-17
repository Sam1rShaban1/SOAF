using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;
using MyApi.Repositories;

// Unit tests for ProductRepository using EF Core InMemory database.
// Tests verify pagination, active-only filtering, and edge case handling.
// Each test uses a unique database name to ensure test isolation.
namespace MyApi.Tests;

public class ProductRepositoryTest
{
    // Creates an isolated InMemory database factory with optional seed data.
    // dbName must be unique per test to prevent test pollution.
    private static IDbContextFactory<AppDbContext> CreateFactory(string dbName, List<Product>? seed = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)     // Unique in-memory store per test.
            .Options;

        // Create schema without seed data from OnModelCreating (HasData removed).
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        context.Dispose();

        // If seed data is provided, add it to the store via a separate context.
        if (seed is not null)
        {
            var seedContext = new AppDbContext(options);
            seedContext.Products.AddRange(seed);
            seedContext.SaveChanges();
            seedContext.Dispose();
        }

        return new TestDbContextFactory(options);
    }

    // Helper factory implementation for creating DbContext instances on demand.
    private class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestDbContextFactory(DbContextOptions<AppDbContext> options) => _options = options;
        public AppDbContext CreateDbContext() => new(_options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken ct = default)
        {
            var db = new AppDbContext(_options);
            return Task.FromResult(db);
        }
    }

    // Generates 10 sample products where only IDs 1-8 are active (9 and 10 are inactive).
    private static List<Product> SampleProducts() =>
        Enumerable.Range(1, 10).Select(i => new Product
        {
            Id = i,
            Name = $"Product {i}",
            Price = i * 10m,
            IsActive = i <= 8       // Products 9 and 10 are inactive.
        }).ToList();

    // -----------------------------------------------------------------------
    // GetAllProductsAsync — Happy Path Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAllProductsAsync_WithValidPagination_ReturnsCorrectPage()
    {
        var factory = CreateFactory(nameof(GetAllProductsAsync_WithValidPagination_ReturnsCorrectPage), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetAllProductsAsync(1, 3);

        Assert.Equal(8, result.TotalCount);      // 8 active products total.
        Assert.Equal(3, result.Items.Count());    // Page 1 of size 3 returns 3 items.
        Assert.Contains(result.Items, p => p.Id == 1);  // First item present.
    }

    [Fact]
    public async Task GetAllProductsAsync_RespectsPagination()
    {
        var factory = CreateFactory(nameof(GetAllProductsAsync_RespectsPagination), SampleProducts());
        var repo = new ProductRepository(factory);

        var page1 = await repo.GetAllProductsAsync(1, 4);
        var page2 = await repo.GetAllProductsAsync(2, 4);

        Assert.Equal(4, page1.Items.Count());     // First page has 4 items.
        Assert.Equal(4, page2.Items.Count());     // Second page has 4 items.
        Assert.DoesNotContain(page1.Items, p => p.Id == 5);   // Id 5 is on page 2.
        Assert.Contains(page2.Items, p => p.Id == 5);         // Id 5 confirmed on page 2.
    }

    [Fact]
    public async Task GetAllProductsAsync_FiltersOnlyActiveProducts()
    {
        var factory = CreateFactory(nameof(GetAllProductsAsync_FiltersOnlyActiveProducts), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetAllProductsAsync(1, 20);  // Large enough to get all.

        Assert.Equal(8, result.TotalCount);                   // Only 8 active products.
        Assert.DoesNotContain(result.Items, p => !p.IsActive);  // No inactive products.
    }

    // -----------------------------------------------------------------------
    // GetAllProductsAsync — Sad Path Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAllProductsAsync_WithPageZero_ReturnsEmpty()
    {
        var factory = CreateFactory(nameof(GetAllProductsAsync_WithPageZero_ReturnsEmpty), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetAllProductsAsync(0, 10);  // Invalid page.

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllProductsAsync_WithNegativePage_ReturnsEmpty()
    {
        var factory = CreateFactory(nameof(GetAllProductsAsync_WithNegativePage_ReturnsEmpty), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetAllProductsAsync(-1, 10);  // Invalid page.

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllProductsAsync_WithEmptyDatabase_ReturnsEmpty()
    {
        var factory = CreateFactory(nameof(GetAllProductsAsync_WithEmptyDatabase_ReturnsEmpty));  // No seed data.
        var repo = new ProductRepository(factory);

        var result = await repo.GetAllProductsAsync(1, 10);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetAllProductsAsync_WithPageBeyondRange_ReturnsEmpty()
    {
        var factory = CreateFactory(nameof(GetAllProductsAsync_WithPageBeyondRange_ReturnsEmpty), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetAllProductsAsync(100, 10);  // Page far beyond available data.

        Assert.Empty(result.Items);
        Assert.Equal(8, result.TotalCount);  // Total count still reflects all active products.
    }

    // -----------------------------------------------------------------------
    // GetProductByIdAsync — Happy Path Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProductByIdAsync_WithValidId_ReturnsProduct()
    {
        var factory = CreateFactory(nameof(GetProductByIdAsync_WithValidId_ReturnsProduct), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetProductByIdAsync(3);

        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
        Assert.Equal("Product 3", result.Name);
    }

    [Fact]
    public async Task GetProductByIdAsync_ReturnsOnlyActiveProduct()
    {
        var factory = CreateFactory(nameof(GetProductByIdAsync_ReturnsOnlyActiveProduct), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetProductByIdAsync(9);  // Product 9 is inactive.

        Assert.Null(result);
    }

    // -----------------------------------------------------------------------
    // GetProductByIdAsync — Sad Path Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProductByIdAsync_WithNonExistentId_ReturnsNull()
    {
        var factory = CreateFactory(nameof(GetProductByIdAsync_WithNonExistentId_ReturnsNull), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetProductByIdAsync(999);  // Non-existent Id.

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithNegativeId_ReturnsNull()
    {
        var factory = CreateFactory(nameof(GetProductByIdAsync_WithNegativeId_ReturnsNull), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetProductByIdAsync(-5);  // Invalid negative Id.

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithZeroId_ReturnsNull()
    {
        var factory = CreateFactory(nameof(GetProductByIdAsync_WithZeroId_ReturnsNull), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetProductByIdAsync(0);  // Invalid zero Id.

        Assert.Null(result);
    }
}
