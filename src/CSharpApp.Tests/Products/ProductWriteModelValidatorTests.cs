namespace CSharpApp.Tests.Products;

/// <summary>
/// The validator matters more than usual here: upstream answers a malformed
/// product with a 500 rather than a 400, so these rules are what turn an opaque
/// upstream fault into an accurate response.
/// </summary>
public sealed class ProductWriteModelValidatorTests
{
    private readonly ProductWriteModelValidator _validator = new();

    [Fact]
    public void Validate_Passes_ForAWellFormedProduct()
    {
        Assert.True(_validator.Validate(Valid()).IsValid);
    }

    [Fact]
    public void Validate_ReportsEveryProblemAtOnce()
    {
        // All rules run, so a caller fixing a request sees every problem rather than
        // discovering them one round trip at a time.
        var result = _validator.Validate(new ProductWriteModel());

        Assert.False(result.IsValid);

        var properties = result.Errors.Select(error => error.PropertyName).Distinct().ToList();

        Assert.Contains(nameof(ProductWriteModel.Title), properties);
        Assert.Contains(nameof(ProductWriteModel.Price), properties);
        Assert.Contains(nameof(ProductWriteModel.Description), properties);
        Assert.Contains(nameof(ProductWriteModel.CategoryId), properties);
        Assert.Contains(nameof(ProductWriteModel.Images), properties);
    }

    [Fact]
    public void Validate_RejectsAMissingPrice()
    {
        // The reason Price is nullable on the write model. A non-nullable decimal
        // would bind an absent price to 0 and silently create a free product, so
        // "absent" has to be distinguishable from "zero".
        var result = _validator.Validate(Valid() with { Price = null });

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RejectsANonPositivePrice(decimal price)
    {
        Assert.False(_validator.Validate(Valid() with { Price = price }).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsAnEmptyTitle(string? title)
    {
        Assert.False(_validator.Validate(Valid() with { Title = title }).IsValid);
    }

    [Fact]
    public void Validate_RejectsAnEmptyImageCollection()
    {
        Assert.False(_validator.Validate(Valid() with { Images = [] }).IsValid);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("/relative/path.png")]
    [InlineData("ftp://example.test/a.png")]
    [InlineData("javascript:alert(1)")]
    public void Validate_RejectsImagesThatAreNotHttpUrls(string image)
    {
        // Relative paths and non-http schemes are rejected deliberately: these
        // values are echoed back to clients, so a javascript: URL would be stored
        // and served.
        var result = _validator.Validate(Valid() with { Images = [image] });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.StartsWith("Images", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("http://example.test/a.png")]
    [InlineData("https://example.test/a.png")]
    public void Validate_AcceptsHttpAndHttpsImages(string image)
    {
        Assert.True(_validator.Validate(Valid() with { Images = [image] }).IsValid);
    }

    [Fact]
    public void Validate_RejectsAnOverlongTitle()
    {
        Assert.False(_validator.Validate(Valid() with { Title = new string('a', 201) }).IsValid);
    }

    private static ProductWriteModel Valid() => new()
    {
        Title = "A product",
        Price = 9.99m,
        Description = "A description",
        CategoryId = 1,
        Images = ["https://example.test/a.png"],
    };
}
