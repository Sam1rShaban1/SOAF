using Microsoft.AspNetCore.Mvc;
using MyApi.DTOs;
using MyApi.Services;

// Controller layer handling HTTP request/response for the Products API.
// Endpoints:
//   GET  /api/products          — paginated list of products
//   GET  /api/products/{id}     — single product by Id
// Caching is applied at both the controller level ([ResponseCache]) and service level (IMemoryCache).
namespace MyApi.Controllers;

[ApiController]                              // Enables automatic model validation, binding, and 400 responses.
[Route("api/[controller]")]                  // Routes to "api/products".
[ResponseCache(Duration = 30, VaryByQueryKeys = ["pageNumber", "pageSize"])]  // HTTP-level cache for list endpoint.
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service;
    }

    // GET /api/products?pageNumber=1&pageSize=10
    // Returns a paginated list of active products as a PagedResult.
    // Validates that pageNumber and pageSize are positive integers.
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] int pageNumber = 1,       // Default to page 1 if not specified.
        [FromQuery] int pageSize = 10,         // Default to 10 items per page.
        CancellationToken ct = default)        // Enables graceful request cancellation.
    {
        // Input validation: reject non-positive pagination values with 400 BadRequest.
        if (pageNumber <= 0 || pageSize <= 0)
            return BadRequest("Page number and page size must be greater than zero.");

        // Delegate to service layer which handles caching and repository calls.
        var result = await _service.GetProductsAsync(pageNumber, pageSize, ct);
        return Ok(result);
    }

    // GET /api/products/{id}
    // Returns a single product by Id, or 404 if not found.
    // Validates that Id is a positive integer.
    [HttpGet("{id}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]  // 60-second HTTP cache for single product.
    public async Task<ActionResult<ProductDto>> GetProduct(int id, CancellationToken ct = default)
    {
        // Input validation: reject non-positive Ids with 400 BadRequest.
        if (id <= 0)
            return BadRequest("Id must be greater than zero.");

        // Delegate to service layer.
        var product = await _service.GetProductAsync(id, ct);

        // Return 404 if the product does not exist or is inactive.
        if (product is null)
            return NotFound();

        return Ok(product);
    }
}
