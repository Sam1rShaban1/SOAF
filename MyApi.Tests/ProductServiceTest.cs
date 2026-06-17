using Microsoft.Extensions.Caching.Memory;
using MyApi.DTOs;
using MyApi.Models;
using MyApi.Repositories;
using MyApi.Services;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

// Unit tests for ProductService using NSubstitute mocks for IProductRepository.
// Tests verify correct delegation to repository, caching behavior, and edge case handling.
namespace MyApi.Tests;

public class ProductServiceTest
{
    private readonly IProductRepository _repo;
    private readonly ProductService _service;

    public ProductServiceTest()
    {
        // Create a mock repository that records method calls for verification.
        _repo = Substitute.For<IProductRepository>();
        // Use a real MemoryCache instance to test caching behavior end-to-end.
        var cache = new MemoryCache(new MemoryCacheOptions());
        _service = new ProductService(_repo, cache);
    }

    // Generates 5 active sample products for test data.
    private static List<Product> SampleProducts() =>
        Enumerable.Range(1, 5).Select(i => new Product
        {
            Id = i,
            Name = $"Product {i}",
            Price = i * 10m,
            IsActive = true
        }).ToList();

    // -----------------------------------------------------------------------
    // GetProductsAsync — Happy Path Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProductsAsync_CallsRepositoryWithCorrectParameters()
    {
        var products = SampleProducts();
        _repo.GetAllProductsAsync(2, 5, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Product> { Items = products, TotalCount = 20, PageNumber = 2, PageSize = 5 });

        await _service.GetProductsAsync(2, 5);

        // Verify the service passed the exact page and pageSize to the repository.
        await _repo.Received(1).GetAllProductsAsync(2, 5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProductsAsync_ReturnsRepositoryDataUnchanged()
    {
        var products = SampleProducts();
        _repo.GetAllProductsAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Product> { Items = products, TotalCount = 5, PageNumber = 1, PageSize = 10 });

        var result = await _service.GetProductsAsync(1, 10);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(5, result.Items.Count());
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    // -----------------------------------------------------------------------
    // GetProductsAsync — Sad Path Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProductsAsync_WithEmptyRepository_ReturnsEmpty()
    {
        _repo.GetAllProductsAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Product> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 10 });

        var result = await _service.GetProductsAsync(1, 10);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    // -----------------------------------------------------------------------
    // GetProductsAsync — Caching Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProductsAsync_UsesCacheOnSecondCall()
    {
        var products = SampleProducts();
        _repo.GetAllProductsAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Product> { Items = products, TotalCount = 5, PageNumber = 1, PageSize = 10 });

        // First call: cache miss, should call repository.
        await _service.GetProductsAsync(1, 10);
        // Second call: cache hit, should NOT call repository again.
        await _service.GetProductsAsync(1, 10);

        // Repository should have been called exactly once despite two service calls.
        await _repo.Received(1).GetAllProductsAsync(1, 10, Arg.Any<CancellationToken>());
    }

    // -----------------------------------------------------------------------
    // GetProductAsync — Happy Path Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProductAsync_CallsRepositoryWithCorrectId()
    {
        var product = new Product { Id = 3, Name = "Product 3", Price = 30m, IsActive = true };
        _repo.GetProductByIdAsync(3, Arg.Any<CancellationToken>()).Returns(product);

        await _service.GetProductAsync(3);

        await _repo.Received(1).GetProductByIdAsync(3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProductAsync_ReturnsProductWhenFound()
    {
        var product = new Product { Id = 3, Name = "Product 3", Price = 30m, IsActive = true };
        _repo.GetProductByIdAsync(3, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _service.GetProductAsync(3);

        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
        Assert.Equal("Product 3", result.Name);
        Assert.Equal(30m, result.Price);
    }

    // -----------------------------------------------------------------------
    // GetProductAsync — Sad Path Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProductAsync_ReturnsNullWhenRepositoryReturnsNull()
    {
        _repo.GetProductByIdAsync(999, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _service.GetProductAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductAsync_HandlesRepositoryException()
    {
        _repo.GetProductByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Product?>(new InvalidOperationException("DB error")));

        // Exception from repository should propagate through the service layer.
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetProductAsync(1));
    }

    // -----------------------------------------------------------------------
    // GetProductAsync — Caching Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProductAsync_UsesCacheOnSecondCall()
    {
        var product = new Product { Id = 1, Name = "Product 1", Price = 10m, IsActive = true };
        _repo.GetProductByIdAsync(1, Arg.Any<CancellationToken>()).Returns(product);

        // First call: cache miss.
        await _service.GetProductAsync(1);
        // Second call: cache hit.
        await _service.GetProductAsync(1);

        // Repository should be called only once.
        await _repo.Received(1).GetProductByIdAsync(1, Arg.Any<CancellationToken>());
    }
}
