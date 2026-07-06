using Xunit;

namespace CSharpApp.Tests;

public class CreateCategoryValidatorTests
{
    [Fact]
    public void Validate_ReturnsInvalid_WhenRequestIsNull()
    {
        // Arrange
        var validator = new CSharpApp.Api.Validation.CreateCategoryValidator();

        // Act
        var ok = validator.Validate(null!, out var errors);

        // Assert
        Assert.False(ok);
        Assert.Contains("Request cannot be null.", errors);
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenNameMissing()
    {
        // Arrange
        var validator = new CSharpApp.Api.Validation.CreateCategoryValidator();
        var request = new CSharpApp.Core.Dtos.CreateCategoryRequestDto { Name = null };

        // Act
        var ok = validator.Validate(request, out var errors);

        // Assert
        Assert.False(ok);
        Assert.Contains("Name is required.", errors);
    }

    [Fact]
    public void Validate_ReturnsValid_WhenNameProvided()
    {
        // Arrange
        var validator = new CSharpApp.Api.Validation.CreateCategoryValidator();
        var request = new CSharpApp.Core.Dtos.CreateCategoryRequestDto { Name = "Books" };

        // Act
        var ok = validator.Validate(request, out var errors);

        // Assert
        Assert.True(ok);
        Assert.Empty(errors);
    }
}
