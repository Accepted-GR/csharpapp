using System.Net.Http.Json;

namespace CSharpApp.Application.Auth
{
	public class AuthApiClient : IAuthApiClient
	{
		private readonly HttpClient _httpClient;
		private readonly RestApiSettings _settings;
		private readonly JsonSerializerOptions _jsonOptions;

		public AuthApiClient(HttpClient httpClient, IOptions<RestApiSettings> settings)
		{
			_httpClient = httpClient;
			_settings = settings.Value;

			_httpClient.BaseAddress = new Uri(_settings.BaseUrl!);
			_jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
		}

		public async Task<AuthTokens> Login(CancellationToken cancellationToken)
		{
			var payload = new { email = _settings.Username, password = _settings.Password };
			var response = await _httpClient.PostAsJsonAsync(_settings.Auth, payload, _jsonOptions, cancellationToken);
			response.EnsureSuccessStatusCode();

			return await ReadTokens(response, cancellationToken);
		}

		public async Task<AuthTokens?> Refresh(string refreshToken, CancellationToken cancellationToken)
		{
			var payload = new { refreshToken };
			var response = await _httpClient.PostAsJsonAsync(_settings.RefreshToken, payload, _jsonOptions, cancellationToken);

			if (!response.IsSuccessStatusCode)
				return null;

			return await ReadTokens(response, cancellationToken);
		}

		private static async Task<AuthTokens> ReadTokens(HttpResponseMessage response, CancellationToken cancellationToken)
		{
			var content = await response.Content.ReadAsStringAsync(cancellationToken);
			var tokens = JsonSerializer.Deserialize<AuthTokens>(content);

			if (tokens is null || string.IsNullOrEmpty(tokens.AccessToken))
				throw new InvalidOperationException("The auth response did not contain an access token.");

			return tokens;
		}
	}
}