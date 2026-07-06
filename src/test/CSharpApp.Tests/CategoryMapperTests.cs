using Xunit;

namespace CSharpApp.Tests;

public class CategoryMapperTests
{
    [Fact]
    public void MapFromCreateRequest_MapsNameAndImage()
    {
        // Arrange
        var mapper = new CSharpApp.Application.Categories.Mapping.CategoryMapper();
        var request = new CSharpApp.Core.Dtos.CreateCategoryRequestDto { Name = "Books", Image = "/books.png" };

        // Act
        var category = mapper.MapFromCreateRequest(request);

        // Assert
        Assert.NotNull(category);
        Assert.Equal("Books", category.Name);
        Assert.Equal("/books.png", category.Image);
    }
}
