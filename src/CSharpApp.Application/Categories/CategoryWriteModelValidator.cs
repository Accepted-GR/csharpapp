namespace CSharpApp.Application.Categories;

/// <summary>
/// Validates a category write request before it reaches the upstream API.
/// </summary>
/// <remarks>
/// Upstream answers a category with no name with a 500, so the same reasoning as
/// <see cref="Products.ProductWriteModelValidator"/> applies here.
/// </remarks>
public sealed class CategoryWriteModelValidator : AbstractValidator<CategoryWriteModel>
{
    public CategoryWriteModelValidator()
    {
        RuleFor(category => category.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(category => category.Image)
            .NotEmpty()
            .Must(BeAnAbsoluteUrl)
            .WithMessage("Image must be an absolute http or https URL.");
    }

    private static bool BeAnAbsoluteUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
