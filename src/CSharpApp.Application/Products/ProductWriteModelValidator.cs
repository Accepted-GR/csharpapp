namespace CSharpApp.Application.Products;

/// <summary>
/// Validates a product write request before it reaches the upstream API.
/// </summary>
/// <remarks>
/// Not merely defensive. The upstream API answers a malformed product with
/// <c>500 Internal server error</c> rather than a 400 -- a missing price, or a PUT
/// with a partial body, both produce one. Validating here turns what would surface
/// as an opaque upstream fault into an accurate 400 with per-field messages, and
/// saves a pointless round trip.
/// </remarks>
public sealed class ProductWriteModelValidator : AbstractValidator<ProductWriteModel>
{
    public ProductWriteModelValidator()
    {
        RuleFor(product => product.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(product => product.Price)
            .NotNull()
            .GreaterThan(0)
            .WithMessage("Price is required and must be greater than zero.");

        RuleFor(product => product.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(product => product.CategoryId)
            .NotNull()
            .GreaterThan(0)
            .WithMessage("CategoryId is required and must be a positive integer.");

        RuleFor(product => product.Images)
            .NotNull()
            .Must(images => images is not null && images.Count > 0)
            .WithMessage("At least one image is required.");

        RuleForEach(product => product.Images)
            .Must(BeAnAbsoluteUrl)
            .WithMessage("Each image must be an absolute http or https URL.");
    }

    private static bool BeAnAbsoluteUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
