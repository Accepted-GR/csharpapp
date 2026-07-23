using CSharpApp.Application.Categories.Commands;
using CSharpApp.Application.Categories.Queries;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ApplicationTests.Categories;

public class CategoryHandlerTests
{
	private readonly Mock<ICategoriesService> _categoriesService = new();

	[Fact]
	public async Task GetCategories_returns_the_categories_from_the_service()
	{
		var categories = new List<Category> { new() { Id = 1, Name = "Clothes" } };
		_categoriesService.Setup(s => s.GetCategories(It.IsAny<CancellationToken>())).ReturnsAsync(categories);
		var handler = new GetCategoriesQueryHandler(_categoriesService.Object);

		var response = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

		response.Categories.Should().BeEquivalentTo(categories);
	}

	[Fact]
	public async Task CreateCategory_returns_the_id_from_the_service()
	{
		_categoriesService.Setup(s => s.CreateCategory(It.IsAny<CreateCategoryPayload>(), It.IsAny<CancellationToken>())).ReturnsAsync(7);
		var handler = new CreateCategoryCommandHandler(_categoriesService.Object, NullLogger<CreateCategoryCommandHandler>.Instance);

		var id = await handler.Handle(new CreateCategoryCommand(new CreateCategoryPayload()), CancellationToken.None);

		id.Should().Be(7);
	}
}
