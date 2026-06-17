using Microsoft.AspNetCore.Mvc;
using MyApi.Controllers;
using MyApi.DTOs;
using MyApi.Services;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

// Unit tests for ProductsController using NSubstitute mocks for IProductService.
// Tests verify HTTP status codes, input validation, and correct response types.
namespace MyApi.Tests;

public class ProductsControllerTest
{
    private readonly IProductService _service;
    private readonly ProductsController _controller;

    public ProductsControllerTest()
    {
        // Mock the service layer to isolate controller behavior.
        _service = Substitute.For<IProductService>();
        _controller = new ProductsController(_service);
    }

    // Helper to create a sample paginated result for use in tests.
    private static PagedResult<ProductDto> SamplePagedResult(int page = 1, int pageSize = 10) =>
        new PagedResult<ProductDto>
        {
            Items = Enumerable.Range(1, 3).Select(i => new ProductDto { Id = i, Name = $"Product {i}", Price = i * 10m }),
            TotalCount = 20,
            PageNumber = page,
            PageSize = pageSize
        };

    // -----------------------------------------------------------------------
    // GET /api/products — Happy Path Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProducts_WithValidParameters_Returns200Ok()
    {
        var paged = SamplePagedResult(1, 5);
        _service.GetProductsAsync(1, 5, Arg.Any<CancellationToken>()).Returns(paged);

        var result = await _controller.GetProducts(1, 5);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsType<PagedResult<ProductDto>>(okResult.Value);
        Assert.Equal(3, data.Items.Count());
    }

    [Fact]
    public async Task GetProducts_WithDefaultParameters_Returns200Ok()
    {
        var paged = SamplePagedResult();
        _service.GetProductsAsync(1, 10, Arg.Any<CancellationToken>()).Returns(paged);

        var result = await _controller.GetProducts();  // Uses default params.

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
    }

    // -----------------------------------------------------------------------
    // GET /api/products — Sad Path Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProducts_WithPageZero_Returns400BadRequest()
    {
        var result = await _controller.GetProducts(pageNumber: 0);  // Invalid: page ≤ 0.

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithNegativePage_Returns400BadRequest()
    {
        var result = await _controller.GetProducts(pageNumber: -1);  // Invalid: page ≤ 0.

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetProducts_WithPageSizeZero_Returns400BadRequest()
    {
        var result = await _controller.GetProducts(pageSize: 0);  // Invalid: pageSize ≤ 0.

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetProducts_WithNegativePageSize_Returns400BadRequest()
    {
        var result = await _controller.GetProducts(pageSize: -5);  // Invalid: pageSize ≤ 0.

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // -----------------------------------------------------------------------
    // GET /api/products/{id} — Happy Path Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProduct_WithValidId_Returns200Ok()
    {
        var dto = new ProductDto { Id = 5, Name = "Product 5", Price = 50m };
        _service.GetProductAsync(5, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _controller.GetProduct(5);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsType<ProductDto>(okResult.Value);
        Assert.Equal(5, data.Id);
    }

    // -----------------------------------------------------------------------
    // GET /api/products/{id} — Sad Path Tests
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetProduct_WithZeroId_Returns400BadRequest()
    {
        var result = await _controller.GetProduct(0);  // Invalid: Id ≤ 0.

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetProduct_WithNegativeId_Returns400BadRequest()
    {
        var result = await _controller.GetProduct(-3);  // Invalid: Id ≤ 0.

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetProduct_WhenNotFound_Returns404()
    {
        _service.GetProductAsync(999, Arg.Any<CancellationToken>()).ReturnsNull();  // Not found.

        var result = await _controller.GetProduct(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
