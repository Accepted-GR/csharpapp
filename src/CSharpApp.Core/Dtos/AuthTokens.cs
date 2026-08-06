namespace CSharpApp.Core.Dtos;

/// <summary>
/// Token pair returned by both <c>/auth/login</c> and <c>/auth/refresh-token</c>.
/// </summary>
/// <remarks>
/// The upstream API returns snake_case here, but expects camelCase
/// (<c>refreshToken</c>) in the refresh request body. The property attributes
/// spell both out rather than relying on a global naming policy that would be
/// wrong for one of the two directions.
/// </remarks>
public sealed record AuthTokens
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }
}
