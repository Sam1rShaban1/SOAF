using MyApi.DTOs;

namespace MyApi.Services;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<ProductDto?> GetProductAsync(int id, CancellationToken ct = default);
}
