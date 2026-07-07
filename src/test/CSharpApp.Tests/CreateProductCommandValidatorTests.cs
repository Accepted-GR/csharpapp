using System.Threading.Tasks;
using FluentValidation;
using Xunit;
using CSharpApp.Application.Products.Commands;
using CSharpApp.Core.Dtos;

namespace CSharpApp.Tests;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator;

    public CreateProductCommandValidatorTests()
    {
        _validator = new CreateProductCommandValidator();
    }

    [Fact]
    public async Task Validate_ReturnsValid_WhenProductIsValid()
    {
        // Arrange
        var command = new CreateProductCommand(
            new Product
            {
                Title = "Test Product",
                Price = 100,
                Description = "Test Description",
                Category = new Category { Id = 1 }
            });

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_ReturnsInvalid_WhenProductIsNull()
    {
        // Arrange
        var command = new CreateProductCommand(null!);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Product cannot be null");
    }

    [Fact]
    public async Task Validate_ReturnsInvalid_WhenTitleIsEmpty()
    {
        // Arrange
        var command = new CreateProductCommand(
            new Product
            {
                Title = "",
                Price = 100
            });

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Title is required");
    }

    [Fact]
    public async Task Validate_ReturnsInvalid_WhenPriceIsZero()
    {
        // Arrange
        var command = new CreateProductCommand(
            new Product
            {
                Title = "Test Product",
                Price = 0
            });

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Price must be greater than zero");
    }

    [Fact]
    public async Task Validate_ReturnsInvalid_WhenCategoryIdIsZero()
    {
        // Arrange
        var command = new CreateProductCommand(
            new Product
            {
                Title = "Test Product",
                Price = 100,
                Category = new Category { Id = 0 }
            });

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "CategoryId must be a positive integer");
    }
}
