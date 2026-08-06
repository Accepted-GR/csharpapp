namespace CSharpApp.Core.Settings;

/// <summary>
/// Validates that a configured path is usable as a relative URI against an
/// <see cref="HttpClient.BaseAddress"/>.
/// </summary>
/// <remarks>
/// A relative URI that begins with '/' is resolved against the host root rather
/// than the base path, so combining a base address of
/// <c>https://host/api/v1/</c> with <c>/categories</c> silently produces
/// <c>https://host/categories</c> — a 404 that looks like an upstream fault
/// rather than a configuration mistake. See finding #2 in docs/AUDIT.md.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RelativePathAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string path)
        {
            // A null value is the concern of [Required], not of this attribute.
            return ValidationResult.Success;
        }

        if (path.StartsWith('/'))
        {
            return new ValidationResult(
                $"'{validationContext.MemberName}' must not start with '/'. A leading slash " +
                "resolves against the host root and discards the base path. " +
                $"Use '{path.TrimStart('/')}' instead.",
                [validationContext.MemberName!]);
        }

        if (!Uri.TryCreate(path, UriKind.Relative, out _))
        {
            return new ValidationResult(
                $"'{validationContext.MemberName}' is not a valid relative URI.",
                [validationContext.MemberName!]);
        }

        return ValidationResult.Success;
    }
}
