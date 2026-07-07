using System.Threading.Tasks;
using FluentValidation;
using Xunit;
using CSharpApp.Application.Categories.Commands;
using CSharpApp.Core.Dtos;

namespace CSharpApp.Tests;

public class CreateCategoryCommandValidatorTests
{
    private readonly CreateCategoryCommandValidator _validator;

    public CreateCategoryCommandValidatorTests()
    {
        _validator = new CreateCategoryCommandValidator();
    }

    [Fact]
    public async Task Validate_ReturnsValid_WhenCategoryIsValid()
    {
        // Arrange
        var command = new CreateCategoryCommand(
            new Category
            {
                Name = "Test Category",
                Image = "https://example.com/image.jpg"
            });

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_ReturnsInvalid_WhenCategoryIsNull()
    {
        // Arrange
        var command = new CreateCategoryCommand(null!);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Category cannot be null");
    }

    [Fact]
    public async Task Validate_ReturnsInvalid_WhenNameIsEmpty()
    {
        // Arrange
        var command = new CreateCategoryCommand(
            new Category
            {
                Name = ""
            });

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Category name is required");
    }

    [Fact]
    public async Task Validate_ReturnsValid_WhenImageIsNull()
    {
        // Arrange
        var command = new CreateCategoryCommand(
            new Category
            {
                Name = "Test Category",
                Image = null
            });

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }
}
