using Microsoft.Extensions.Caching.Memory;
using MyApi.DTOs;
using MyApi.Repositories;

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

    public async Task<PagedResult<ProductDto>> GetProductsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var cacheKey = $"products_all_p{page}_s{pageSize}";

        if (_cache.TryGetValue(cacheKey, out PagedResult<ProductDto>? cached))
            return cached!;

        var result = await _repository.GetAllProductsAsync(page, pageSize, ct).ConfigureAwait(false);

        var dtoItems = result.Items.Select(p => new ProductDto(p.Id, p.Name, p.Price));
        var paged = new PagedResult<ProductDto>(dtoItems, result.TotalCount, result.PageNumber, result.PageSize);

        _cache.Set(cacheKey, paged, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(1)
        });

        return paged;
    }

    public async Task<ProductDto?> GetProductAsync(int id, CancellationToken ct = default)
    {
        var cacheKey = $"product_{id}";

        if (_cache.TryGetValue(cacheKey, out ProductDto? cached))
            return cached;

        var product = await _repository.GetProductByIdAsync(id, ct).ConfigureAwait(false);

        if (product is null)
            return null;

        var dto = new ProductDto(product.Id, product.Name, product.Price);

        _cache.Set(cacheKey, dto, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(1)
        });

        return dto;
    }
}
