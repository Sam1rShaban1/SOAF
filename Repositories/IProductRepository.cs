using MyApi.DTOs;
using MyApi.Models;

namespace MyApi.Repositories;

public interface IProductRepository
{
    Task<PagedResult<Product>> GetAllProductsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default);
}
