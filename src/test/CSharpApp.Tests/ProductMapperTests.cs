using Xunit;

namespace CSharpApp.Tests;

public class ProductMapperTests
{
    [Fact]
    public void MapFromCreateRequest_MapsFields()
    {
        // Arrange
        var mapper = new CSharpApp.Application.Products.ProductMapper();
        var request = new CSharpApp.Core.Dtos.CreateProductRequestDto
        {
            Title = "New",
            Price = 20m,
            Description = "Desc",
            Images = new System.Collections.Generic.List<string> { "/1.png" },
            CategoryId = 2
        };

        // Act
        var product = mapper.MapFromCreateRequest(request);

        // Assert
        Assert.NotNull(product);
        Assert.Equal("New", product.Title);
        Assert.Equal(20m, product.Price);
        Assert.Equal("Desc", product.Description);
        Assert.Equal(1, product.Images.Count);
        Assert.Equal(2, product.Category?.Id);
    }
}
