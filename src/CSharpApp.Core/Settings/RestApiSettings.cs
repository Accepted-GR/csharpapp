namespace CSharpApp.Core.Settings;

public sealed class RestApiSettings
{
    /// <summary>
    /// Absolute base address of the upstream API. Must end with a trailing slash,
    /// otherwise <see cref="Uri"/> resolution discards the last path segment.
    /// </summary>
    [Required]
    [Url]
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Relative paths, resolved against <see cref="BaseUrl"/>. A leading slash
    /// would resolve against the host root instead of the base path, so it is
    /// rejected by <see cref="RelativePathAttribute"/>.
    /// </summary>
    [Required]
    [RelativePath]
    public string? Products { get; set; }

    [Required]
    [RelativePath]
    public string? Categories { get; set; }

    [Required]
    [RelativePath]
    public string? Auth { get; set; }

    [Required]
    [RelativePath]
    public string? RefreshToken { get; set; }

    [Required]
    [EmailAddress]
    public string? Username { get; set; }

    [Required]
    public string? Password { get; set; }
}
