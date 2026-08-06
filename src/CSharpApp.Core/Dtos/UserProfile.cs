namespace CSharpApp.Core.Dtos;

/// <summary>
/// The authenticated user, as returned by <c>/auth/profile</c>.
/// </summary>
/// <remarks>
/// The upstream response also contains a plaintext <c>password</c> field. It is
/// deliberately not modelled here: anything mapped onto this type is serialized
/// straight back to our own callers, and echoing a credential -- even one from a
/// public demo API -- is not a habit worth forming. Unmapped JSON properties are
/// ignored by System.Text.Json, so leaving it out is sufficient to drop it.
/// </remarks>
public sealed record UserProfile
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("role")]
    public string? Role { get; init; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; init; }
}
