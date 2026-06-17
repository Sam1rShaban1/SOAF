using MyApi.DTOs;
using MyApi.Models;

// Repository interface defining data access contracts for Product entities.
// Abstracts the underlying data store so the service layer does not depend on EF Core directly.
namespace MyApi.Repositories;

public interface IProductRepository
{
    // Retrieves a paginated subset of active products, ordered by Id.
    // page: 1-based page number, pageSize: number of items per page.
    Task<PagedResult<Product>> GetAllProductsAsync(int page, int pageSize, CancellationToken ct = default);

    // Retrieves a single active product by its unique identifier.
    // Returns null if no active product with the given Id exists.
    Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default);
}
