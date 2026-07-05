using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Interfaces;

namespace CSharpApp.Infrastructure.Authentication;

public class TokenService : ITokenService
{
    private readonly HttpClient _httpClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TokenService> _logger;

    private const string CacheKey = "ThirdPartyAccessToken";

    public TokenService(HttpClient httpClient, IOptions<RestApiSettings> restApiSettings,
        IMemoryCache cache, ILogger<TokenService> logger)
    {
        _httpClient = httpClient;
        _restApiSettings = restApiSettings.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<string>(CacheKey, out var token) && !string.IsNullOrWhiteSpace(token))
        {
            return token;
        }

        // Acquire token
        if (string.IsNullOrWhiteSpace(_restApiSettings.Auth) || string.IsNullOrWhiteSpace(_restApiSettings.Username) || string.IsNullOrWhiteSpace(_restApiSettings.Password))
        {
            _logger.LogError("Auth settings are not configured properly");
            throw new InvalidOperationException("Auth settings are not configured");
        }

        var payload = new { username = _restApiSettings.Username, password = _restApiSettings.Password };
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_restApiSettings.Auth, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to acquire token from auth endpoint. StatusCode: {StatusCode}", response.StatusCode);
            throw new InvalidOperationException("Failed to acquire token from auth endpoint");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.access_token))
        {
            _logger.LogError("Auth endpoint returned invalid token response: {Response}", responseContent);
            throw new InvalidOperationException("Invalid token response from auth endpoint");
        }

        var expiresIn = tokenResponse.expires_in ?? 3600;
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Math.Max(60, expiresIn - 60))
        };

        _cache.Set(CacheKey, tokenResponse.access_token, cacheEntryOptions);

        return tokenResponse.access_token;
    }
}
