using MyApi.DTOs;

// Service interface defining business logic contracts for product operations.
// Acts as an intermediary between the controller and repository layers,
// enabling caching, validation, and DTO mapping without coupling to the data layer.
namespace MyApi.Services;

public interface IProductService
{
    // Retrieves a paginated list of products as DTOs.
    // Coordinates caching, repository calls, and model-to-DTO mapping.
    Task<PagedResult<ProductDto>> GetProductsAsync(int page, int pageSize, CancellationToken ct = default);

    // Retrieves a single product DTO by Id, or null if not found.
    Task<ProductDto?> GetProductAsync(int id, CancellationToken ct = default);
}
