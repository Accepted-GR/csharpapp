using CSharpApp.Application.Products.Commands;
using CSharpApp.Application.Products.Queries;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ApplicationTests.Products;

public class ProductCommandAndQueryHandlerTests
{
	private readonly Mock<IProductsService> _productsService = new();

	[Fact]
	public async Task GetProductById_wraps_the_service_result()
	{
		var product = new Product { Id = 5, Title = "A" };
		_productsService.Setup(s => s.GetProductById(5, It.IsAny<CancellationToken>())).ReturnsAsync(product);
		var handler = new GetProductByIdQueryHandler(_productsService.Object);

		var response = await handler.Handle(new GetProductByIdQuery(5), CancellationToken.None);

		response.Product.Should().Be(product);
	}

	[Fact]
	public async Task GetProductById_returns_a_null_product_when_not_found()
	{
		_productsService.Setup(s => s.GetProductById(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
		var handler = new GetProductByIdQueryHandler(_productsService.Object);

		var response = await handler.Handle(new GetProductByIdQuery(999), CancellationToken.None);

		response.Product.Should().BeNull();
	}

	[Fact]
	public async Task CreateProduct_returns_the_id_from_the_service()
	{
		_productsService.Setup(s => s.CreateProduct(It.IsAny<CreateProductPayload>(), It.IsAny<CancellationToken>())).ReturnsAsync(42);
		var handler = new CreateProductCommandHandler(_productsService.Object, NullLogger<CreateProductCommandHandler>.Instance);

		var id = await handler.Handle(new CreateProductCommand(new CreateProductPayload()), CancellationToken.None);

		id.Should().Be(42);
	}

	[Fact]
	public async Task CreateProduct_returns_null_when_the_service_returns_null()
	{
		_productsService.Setup(s => s.CreateProduct(It.IsAny<CreateProductPayload>(), It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);
		var handler = new CreateProductCommandHandler(_productsService.Object, NullLogger<CreateProductCommandHandler>.Instance);

		var id = await handler.Handle(new CreateProductCommand(new CreateProductPayload()), CancellationToken.None);

		id.Should().BeNull();
	}
}
