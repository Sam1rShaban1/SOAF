using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.DTOs;
using MyApi.Models;

// Concrete implementation of IProductRepository using Entity Framework Core with SQLite.
// Query optimization: uses AsNoTracking() for read-only queries to avoid change-tracking overhead,
// and applies filtering/sorting/pagination at the database level via IQueryable before materialization.
namespace MyApi.Repositories;

public class ProductRepository : IProductRepository
{
    // Uses IDbContextFactory to create short-lived DbContext instances,
    // avoiding concurrency issues with singleton-scoped repositories.
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ProductRepository(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    // Retrieves a paginated list of active products.
    // Database-level filtering: Where(IsActive), OrderBy, Skip/Take — all executed in SQL.
    // Returns early with empty result for invalid pagination parameters.
    public async Task<PagedResult<Product>> GetAllProductsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        // Create a fresh DbContext for this operation and dispose it when done.
        await using var db = await _factory.CreateDbContextAsync(ct);

        // Guard clause: invalid pagination values return an empty result immediately.
        if (page <= 0 || pageSize <= 0)
        {
            return new PagedResult<Product> { Items = [], TotalCount = 0, PageNumber = page, PageSize = pageSize };
        }

        // Build the query: read-only, active only, ordered by Id.
        var query = db.Products
            .AsNoTracking()                     // No change tracking — faster reads.
            .Where(p => p.IsActive)              // Database-level soft-delete filter.
            .OrderBy(p => p.Id);                 // Stable sort order for pagination.

        // Execute count query separately (still at DB level) for total item count.
        var total = await query.CountAsync(ct);

        // Apply pagination and materialize only the requested page.
        var items = await query
            .Skip((page - 1) * pageSize)         // Offset based on page number.
            .Take(pageSize)                      // Limit to page size.
            .ToListAsync(ct);                     // Materialize to List<Product>.

        return new PagedResult<Product> { Items = items, TotalCount = total, PageNumber = page, PageSize = pageSize };
    }

    // Retrieves a single active product by Id, or null if not found or inactive.
    // Database-level filtering ensures only active products are considered.
    public async Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Products
            .AsNoTracking()                      // Read-only query optimization.
            .Where(p => p.IsActive)              // Only return active products.
            .FirstOrDefaultAsync(p => p.Id == id, ct);  // Materialize or null.
    }
}
