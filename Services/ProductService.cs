using Microsoft.Extensions.Caching.Memory;
using MyApi.DTOs;
using MyApi.Repositories;

// Service layer implementation handling business logic, caching, and DTO mapping.
// Uses IMemoryCache to reduce redundant repository calls for frequently accessed data.
// Caching strategy: composite keys for paginated results, individual keys for single products.
namespace MyApi.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IMemoryCache _cache;

    public ProductService(IProductRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    // Returns paginated product DTOs. Caches results per page/size combination.
    // Cache keys: "products_all_p{page}_s{pageSize}" with 5min absolute + 1min sliding expiration.
    public async Task<PagedResult<ProductDto>> GetProductsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        // Build a unique cache key based on pagination parameters.
        var cacheKey = $"products_all_p{page}_s{pageSize}";

        // Check cache first — return cached result if available.
        if (_cache.TryGetValue(cacheKey, out PagedResult<ProductDto>? cached))
            return cached!;

        // Cache miss: fetch from repository with ConfigureAwait(false) to avoid deadlocks.
        var result = await _repository.GetAllProductsAsync(page, pageSize, ct).ConfigureAwait(false);

        // Map domain models to DTOs (only expose necessary fields).
        var dtoItems = result.Items.Select(p => new ProductDto(p.Id, p.Name, p.Price));
        var paged = new PagedResult<ProductDto>(dtoItems, result.TotalCount, result.PageNumber, result.PageSize);

        // Store in cache with expiration policy.
        _cache.Set(cacheKey, paged, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),   // Hard upper bound.
            SlidingExpiration = TimeSpan.FromMinutes(1)                  // Reset if accessed.
        });

        return paged;
    }

    // Returns a single product DTO by Id. Caches individual product lookups.
    // Cache keys: "product_{id}" with same expiration policy.
    public async Task<ProductDto?> GetProductAsync(int id, CancellationToken ct = default)
    {
        var cacheKey = $"product_{id}";

        // Return from cache if available.
        if (_cache.TryGetValue(cacheKey, out ProductDto? cached))
            return cached;

        // Cache miss: query repository.
        var product = await _repository.GetProductByIdAsync(id, ct).ConfigureAwait(false);

        // Return null immediately if the product was not found (don't cache negatives).
        if (product is null)
            return null;

        // Map to DTO and cache.
        var dto = new ProductDto(product.Id, product.Name, product.Price);

        _cache.Set(cacheKey, dto, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(1)
        });

        return dto;
    }
}
