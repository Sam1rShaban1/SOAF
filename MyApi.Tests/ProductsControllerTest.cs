using Microsoft.AspNetCore.Mvc;
using MyApi.Controllers;
using MyApi.DTOs;
using MyApi.Services;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace MyApi.Tests;

public class ProductsControllerTest
{
    private readonly IProductService _service;
    private readonly ProductsController _controller;

    public ProductsControllerTest()
    {
        _service = Substitute.For<IProductService>();
        _controller = new ProductsController(_service);
    }

    private static PagedResult<ProductDto> SamplePagedResult(int page = 1, int pageSize = 10) =>
        new(
            Enumerable.Range(1, 3).Select(i => new ProductDto(i, $"Product {i}", i * 10m)),
            TotalCount: 20,
            PageNumber: page,
            PageSize: pageSize
        );

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

        var result = await _controller.GetProducts();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithPageZero_Returns400BadRequest()
    {
        var result = await _controller.GetProducts(pageNumber: 0);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithNegativePage_Returns400BadRequest()
    {
        var result = await _controller.GetProducts(pageNumber: -1);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetProducts_WithPageSizeZero_Returns400BadRequest()
    {
        var result = await _controller.GetProducts(pageSize: 0);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetProducts_WithNegativePageSize_Returns400BadRequest()
    {
        var result = await _controller.GetProducts(pageSize: -5);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetProduct_WithValidId_Returns200Ok()
    {
        var dto = new ProductDto(5, "Product 5", 50m);
        _service.GetProductAsync(5, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _controller.GetProduct(5);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsType<ProductDto>(okResult.Value);
        Assert.Equal(5, data.Id);
    }

    [Fact]
    public async Task GetProduct_WithZeroId_Returns400BadRequest()
    {
        var result = await _controller.GetProduct(0);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetProduct_WithNegativeId_Returns400BadRequest()
    {
        var result = await _controller.GetProduct(-3);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetProduct_WhenNotFound_Returns404()
    {
        _service.GetProductAsync(999, Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _controller.GetProduct(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
