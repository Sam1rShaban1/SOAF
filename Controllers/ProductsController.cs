using Microsoft.AspNetCore.Mvc;
using MyApi.DTOs;
using MyApi.Services;

namespace MyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[ResponseCache(Duration = 30, VaryByQueryKeys = ["pageNumber", "pageSize"])]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (pageNumber <= 0 || pageSize <= 0)
            return BadRequest("Page number and page size must be greater than zero.");

        var result = await _service.GetProductsAsync(pageNumber, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<ProductDto>> GetProduct(int id, CancellationToken ct = default)
    {
        if (id <= 0)
            return BadRequest("Id must be greater than zero.");

        var product = await _service.GetProductAsync(id, ct);
        if (product is null)
            return NotFound();
        return Ok(product);
    }
}
