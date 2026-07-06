using Xunit;

namespace CSharpApp.Tests;

public class ProductValidatorTests
{
    [Fact]
    public void ValidateForCreate_ReturnsInvalid_WhenRequestIsNull()
    {
        // Arrange
        var validator = new CSharpApp.Application.Products.ProductValidator();

        // Act
        var result = validator.ValidateForCreate(null!);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Request cannot be null.", result.Errors);
    }

    [Fact]
    public void ValidateForCreate_ReturnsInvalid_WhenTitleMissing()
    {
        // Arrange
        var request = new CSharpApp.Core.Dtos.CreateProductRequestDto { Title = null };
        var validator = new CSharpApp.Application.Products.ProductValidator();

        // Act
        var result = validator.ValidateForCreate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Title is required.", result.Errors);
    }

    [Fact]
    public void ValidateForCreate_ReturnsInvalid_WhenPriceNonPositive()
    {
        // Arrange
        var request = new CSharpApp.Core.Dtos.CreateProductRequestDto { Title = "T", Price = 0m };
        var validator = new CSharpApp.Application.Products.ProductValidator();

        // Act
        var result = validator.ValidateForCreate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Price must be greater than zero when provided.", result.Errors);
    }

    [Fact]
    public void ValidateForCreate_ReturnsValid_WhenDataIsGood()
    {
        // Arrange
        var request = new CSharpApp.Core.Dtos.CreateProductRequestDto { Title = "T", Price = 10m };
        var validator = new CSharpApp.Application.Products.ProductValidator();

        // Act
        var result = validator.ValidateForCreate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
