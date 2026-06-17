using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.DTOs;
using MyApi.Models;

namespace MyApi.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ProductRepository(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<PagedResult<Product>> GetAllProductsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        if (page <= 0 || pageSize <= 0)
        {
            return new PagedResult<Product>(Enumerable.Empty<Product>(), 0, page, pageSize);
        }

        var query = db.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Product>(items, total, page, pageSize);
    }

    public async Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        return await db.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }
}
