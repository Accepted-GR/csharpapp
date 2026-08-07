namespace CSharpApp.Core.Dtos;

/// <summary>
/// The shape the upstream API expects when creating or replacing a category.
/// </summary>
/// <remarks>
/// Both fields are required even though the upstream PUT tolerates a partial body.
/// PUT is a full replacement by definition, and accepting a partial one here would
/// mean the same verb behaved differently for categories than for products, where
/// upstream rejects a partial body with a 500.
/// </remarks>
public sealed record CategoryWriteModel
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("image")]
    public string? Image { get; init; }
}
