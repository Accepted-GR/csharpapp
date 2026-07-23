using CSharpApp.Application.Products.Queries;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace ApplicationTests.Products;

// Smoke test: proves InternalsVisibleTo (the handler is internal) + xUnit + Moq + FluentAssertions all work.
public class GetProductsQueryHandlerTests
{
	[Fact]
	public async Task Handle_returns_the_products_from_the_service()
	{
		var products = new List<Product> { new() { Id = 1, Title = "Test" } };

		var productsService = new Mock<IProductsService>();
		productsService
			.Setup(s => s.GetProducts(It.IsAny<CancellationToken>()))
			.ReturnsAsync(products);

		var handler = new GetProductsQueryHandler(productsService.Object);

		var response = await handler.Handle(new GetProductsQuery(), CancellationToken.None);

		response.Products.Should().BeEquivalentTo(products);
		productsService.Verify(s => s.GetProducts(It.IsAny<CancellationToken>()), Times.Once);
	}
}
