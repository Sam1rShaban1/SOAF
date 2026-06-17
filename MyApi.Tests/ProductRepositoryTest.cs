using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;
using MyApi.Repositories;

namespace MyApi.Tests;

public class ProductRepositoryTest
{
    private static IDbContextFactory<AppDbContext> CreateFactory(string dbName, List<Product>? seed = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        context.Dispose();

        if (seed is not null)
        {
            var seedContext = new AppDbContext(options);
            seedContext.Products.AddRange(seed);
            seedContext.SaveChanges();
            seedContext.Dispose();
        }

        return new TestDbContextFactory(options);
    }

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

    private static List<Product> SampleProducts() =>
        Enumerable.Range(1, 10).Select(i => new Product
        {
            Id = i,
            Name = $"Product {i}",
            Price = i * 10m,
            IsActive = i <= 8
        }).ToList();

    [Fact]
    public async Task GetAllProductsAsync_WithValidPagination_ReturnsCorrectPage()
    {
        var factory = CreateFactory(nameof(GetAllProductsAsync_WithValidPagination_ReturnsCorrectPage), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetAllProductsAsync(1, 3);

        Assert.Equal(8, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
        Assert.Contains(result.Items, p => p.Id == 1);
    }

    [Fact]
    public async Task GetAllProductsAsync_RespectsPagination()
    {
        var factory = CreateFactory(nameof(GetAllProductsAsync_RespectsPagination), SampleProducts());
        var repo = new ProductRepository(factory);

        var page1 = await repo.GetAllProductsAsync(1, 4);
        var page2 = await repo.GetAllProductsAsync(2, 4);

        Assert.Equal(4, page1.Items.Count());
        Assert.Equal(4, page2.Items.Count());
        Assert.DoesNotContain(page1.Items, p => p.Id == 5);
        Assert.Contains(page2.Items, p => p.Id == 5);
    }

    [Fact]
    public async Task GetAllProductsAsync_FiltersOnlyActiveProducts()
    {
        var factory = CreateFactory(nameof(GetAllProductsAsync_FiltersOnlyActiveProducts), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetAllProductsAsync(1, 20);

        Assert.Equal(8, result.TotalCount);
        Assert.DoesNotContain(result.Items, p => !p.IsActive);
    }

    [Fact]
    public async Task GetAllProductsAsync_WithPageZero_ReturnsEmpty()
    {
        var factory = CreateFactory(nameof(GetAllProductsAsync_WithPageZero_ReturnsEmpty), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetAllProductsAsync(0, 10);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllProductsAsync_WithNegativePage_ReturnsEmpty()
    {
        var factory = CreateFactory(nameof(GetAllProductsAsync_WithNegativePage_ReturnsEmpty), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetAllProductsAsync(-1, 10);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllProductsAsync_WithEmptyDatabase_ReturnsEmpty()
    {
        var factory = CreateFactory(nameof(GetAllProductsAsync_WithEmptyDatabase_ReturnsEmpty));
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

        var result = await repo.GetAllProductsAsync(100, 10);

        Assert.Empty(result.Items);
        Assert.Equal(8, result.TotalCount);
    }

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

        var result = await repo.GetProductByIdAsync(9);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithNonExistentId_ReturnsNull()
    {
        var factory = CreateFactory(nameof(GetProductByIdAsync_WithNonExistentId_ReturnsNull), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetProductByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithNegativeId_ReturnsNull()
    {
        var factory = CreateFactory(nameof(GetProductByIdAsync_WithNegativeId_ReturnsNull), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetProductByIdAsync(-5);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithZeroId_ReturnsNull()
    {
        var factory = CreateFactory(nameof(GetProductByIdAsync_WithZeroId_ReturnsNull), SampleProducts());
        var repo = new ProductRepository(factory);

        var result = await repo.GetProductByIdAsync(0);

        Assert.Null(result);
    }
}
