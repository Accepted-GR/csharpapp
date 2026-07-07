using Xunit;

namespace CSharpApp.Tests;

public class CategoryValidatorTests
{
    [Fact]
    public void ValidateForCreate_ReturnsInvalid_WhenRequestIsNull()
    {
        // Arrange
        CSharpApp.Application.Categories.Validation.CategoryValidator validator = new CSharpApp.Application.Categories.Validation.CategoryValidator();

        // Act
        var result = validator.ValidateForCreate(null!);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Request cannot be null.", result.Errors);
    }

    [Fact]
    public void ValidateForCreate_ReturnsInvalid_WhenNameMissing()
    {
        // Arrange
        var request = new CSharpApp.Core.Dtos.CreateCategoryRequestDto { Name = null };
        CSharpApp.Application.Categories.Validation.CategoryValidator validator = new CSharpApp.Application.Categories.Validation.CategoryValidator();

        // Act
        var result = validator.ValidateForCreate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Name is required.", result.Errors);
    }

    [Fact]
    public void ValidateForCreate_ReturnsValid_WhenNameProvided()
    {
        // Arrange
        var request = new CSharpApp.Core.Dtos.CreateCategoryRequestDto { Name = "Books" };
        CSharpApp.Application.Categories.Validation.CategoryValidator validator = new CSharpApp.Application.Categories.Validation.CategoryValidator();

        // Act
        var result = validator.ValidateForCreate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
