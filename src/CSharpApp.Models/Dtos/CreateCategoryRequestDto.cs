namespace CSharpApp.Core.Dtos;

using System.Text.Json.Serialization;

public sealed class CreateCategoryRequestDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }
}
