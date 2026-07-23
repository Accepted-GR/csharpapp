using System.Net;
using ApplicationTests.TestSupport;
using CSharpApp.Application.Categories;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ApplicationTests.Categories;

public class CategoriesServiceTests
{
	private static RestApiSettings Settings() => new() { BaseUrl = "https://example.com/", Categories = "categories" };

	private static CategoriesService CreateSut(StubHttpMessageHandler stub)
		=> new(new HttpClient(stub), Options.Create(Settings()), NullLogger<CategoriesService>.Instance);

	[Fact]
	public async Task GetCategories_parses_the_downstream_payload()
	{
		var stub = new StubHttpMessageHandler(
			StubHttpMessageHandler.Json(HttpStatusCode.OK, "[{\"id\":1,\"name\":\"Clothes\"}]"));

		var categories = await CreateSut(stub).GetCategories(CancellationToken.None);

		categories.Should().ContainSingle().Which.Name.Should().Be("Clothes");
	}

	[Fact]
	public async Task GetCategoryById_returns_null_when_not_found()
	{
		var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));

		var category = await CreateSut(stub).GetCategoryById(999, CancellationToken.None);

		category.Should().BeNull();
	}

	[Fact]
	public async Task CreateCategory_returns_the_new_id()
	{
		var stub = new StubHttpMessageHandler(
			StubHttpMessageHandler.Json(HttpStatusCode.Created, "{\"id\":7,\"name\":\"New\"}"));
		var payload = new CreateCategoryPayload { Name = "New", Image = "https://x/i.jpg" };

		var id = await CreateSut(stub).CreateCategory(payload, CancellationToken.None);

		id.Should().Be(7);
	}
}
