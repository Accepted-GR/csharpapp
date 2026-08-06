namespace CSharpApp.Core.Dtos;

public sealed class Product
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    /// <summary>
    /// Monetary value, so <see cref="decimal"/> rather than the original
    /// <see cref="int"/>. The upstream API happens to return whole numbers today,
    /// but a price is not an integer and truncating one silently is worse than
    /// carrying the extra precision.
    /// </summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Needs a setter. System.Text.Json defaults to
    /// <see cref="JsonObjectCreationHandling.Replace"/>, which skips get-only
    /// properties entirely -- the original get-only declaration meant every
    /// upstream image URL was silently dropped. See finding #4 in docs/AUDIT.md.
    /// </summary>
    [JsonPropertyName("images")]
    public IReadOnlyList<string> Images { get; set; } = [];

    [JsonPropertyName("category")]
    public Category? Category { get; set; }

    [JsonPropertyName("creationAt")]
    public DateTimeOffset? CreationAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
