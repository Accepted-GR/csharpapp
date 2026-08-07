namespace CSharpApp.Core.Dtos;

/// <summary>
/// The shape the upstream API expects when creating or replacing a product.
/// </summary>
/// <remarks>
/// Distinct from <see cref="Product"/> because the write contract is not the read
/// contract: writes take a flat <c>categoryId</c> where reads return a nested
/// <c>category</c> object, and server-owned fields (<c>id</c>, <c>slug</c>,
/// <c>creationAt</c>, <c>updatedAt</c>) must not be sent at all.
///
/// Every property is nullable, with the requirements enforced by the validator
/// rather than the type. A non-nullable <c>decimal Price</c> would bind a missing
/// price to 0 and silently create a free product; nullable lets the validator tell
/// "absent" apart from "zero" and report it accurately.
/// </remarks>
public sealed record ProductWriteModel
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("categoryId")]
    public int? CategoryId { get; init; }

    [JsonPropertyName("images")]
    public IReadOnlyList<string>? Images { get; init; }
}
