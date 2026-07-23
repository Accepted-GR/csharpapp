namespace CSharpApp.Core.Dtos
{
	public sealed class AuthTokens
	{
		[JsonPropertyName("access_token")]
		public string AccessToken { get; init; } = string.Empty;

		[JsonPropertyName("refresh_token")]
		public string RefreshToken { get; init; } = string.Empty;
	}
}
