namespace CSharpApp.Core.Dtos;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public sealed class CreateProductRequestDto
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }

    [JsonPropertyName("categoryId")]
    public int? CategoryId { get; set; }
}
