namespace CSharpApp.Application.Categories;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public sealed class UpstreamCreateCategory
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }
}
