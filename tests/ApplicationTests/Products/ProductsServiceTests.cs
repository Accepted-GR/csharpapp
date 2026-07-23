using System.Net;
using ApplicationTests.TestSupport;
using CSharpApp.Application.Products;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ApplicationTests.Products;

public class ProductsServiceTests
{
	private static RestApiSettings Settings() => new() { BaseUrl = "https://example.com/", Products = "products" };

	private static ProductsService CreateSut(StubHttpMessageHandler stub)
		=> new(new HttpClient(stub), Options.Create(Settings()), NullLogger<ProductsService>.Instance);

	[Fact]
	public async Task GetProducts_parses_the_downstream_payload()
	{
		var stub = new StubHttpMessageHandler(
			StubHttpMessageHandler.Json(HttpStatusCode.OK, "[{\"id\":1,\"title\":\"A\"},{\"id\":2,\"title\":\"B\"}]"));

		var products = await CreateSut(stub).GetProducts(CancellationToken.None);

		products.Should().HaveCount(2);
		products.Select(p => p.Id).Should().Equal(1, 2);
	}

	[Fact]
	public async Task GetProducts_returns_empty_when_the_downstream_fails()
	{
		var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));

		var products = await CreateSut(stub).GetProducts(CancellationToken.None);

		products.Should().BeEmpty();
	}

	[Fact]
	public async Task GetProductById_returns_null_when_not_found()
	{
		var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));

		var product = await CreateSut(stub).GetProductById(999, CancellationToken.None);

		product.Should().BeNull();
	}

	[Fact]
	public async Task CreateProduct_returns_the_new_id()
	{
		var stub = new StubHttpMessageHandler(
			StubHttpMessageHandler.Json(HttpStatusCode.Created, "{\"id\":42,\"title\":\"A\"}"));
		var payload = new CreateProductPayload { Title = "A", Price = 1, Description = "d", CategoryId = 1, Images = new[] { "https://x/i.jpg" } };

		var id = await CreateSut(stub).CreateProduct(payload, CancellationToken.None);

		id.Should().Be(42);
	}

	[Fact]
	public async Task CreateProduct_returns_null_when_the_downstream_rejects_it()
	{
		var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
		var payload = new CreateProductPayload { Title = "A", Price = 1, Description = "d", CategoryId = 1, Images = new[] { "https://x/i.jpg" } };

		var id = await CreateSut(stub).CreateProduct(payload, CancellationToken.None);

		id.Should().BeNull();
	}
}
