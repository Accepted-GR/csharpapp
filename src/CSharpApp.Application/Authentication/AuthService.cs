namespace CSharpApp.Application.Authentication;

/// <summary>
/// Acquires, caches and refreshes the upstream access token.
/// </summary>
/// <remarks>
/// Registered as a singleton so the cached token is shared across requests --
/// authenticating once per request would be pointless work against a token that
/// stays valid for days.
///
/// Because it is a singleton it takes <see cref="IHttpClientFactory"/> and creates
/// a client per call, rather than having one injected. A singleton holding an
/// injected <see cref="HttpClient"/> is a captive dependency: it would pin one
/// handler for the lifetime of the application, so <c>SetHandlerLifetime</c> would
/// never rotate it and the connection would eventually go stale on a DNS change --
/// exactly the problem Phase 1 set out to fix. Creating a client is cheap; the
/// handler underneath is pooled and rotated by the factory.
///
/// The named client used here must NOT have the bearer-token handler attached, or
/// acquiring a token would require a token and recurse. That separation is set up
/// in <c>HttpConfiguration</c>.
/// </remarks>
public sealed partial class AuthService : IAuthService, IDisposable
{
    /// <summary>
    /// Name of the deliberately unauthenticated client used to acquire tokens.
    /// </summary>
    public const string HttpClientName = "upstream-auth";

    /// <summary>
    /// How long before actual expiry a token is treated as stale. Covers clock skew
    /// between us and the upstream service, plus the flight time of the request the
    /// token is about to be used on.
    /// </summary>
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Fallback lifetime used when a token carries no readable <c>exp</c> claim.
    /// Deliberately short: re-authenticating too often is a much smaller problem
    /// than trusting a token that has already expired.
    /// </summary>
    private static readonly TimeSpan FallbackLifetime = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RestApiSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthService> _logger;

    // Guards the acquire/refresh path so that concurrent callers arriving on an
    // expired token produce one upstream login rather than one per caller.
    private readonly SemaphoreSlim _gate = new(1, 1);

    private CachedToken? _cached;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        IOptions<RestApiSettings> settings,
        TimeProvider timeProvider,
        ILogger<AuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: a valid cached token needs no synchronisation at all.
        if (TryGetValidToken(out var token))
        {
            return token;
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            // Re-check inside the gate. Several callers can queue here behind a
            // single refresh, and all but the first should use its result.
            if (TryGetValidToken(out token))
            {
                return token;
            }

            var tokens = await AcquireTokensAsync(cancellationToken);

            var accessToken = tokens.AccessToken;

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new AuthenticationFailedException(
                    "The upstream service returned a response with no access token.");
            }

            var declaredExpiry = JwtExpiry.TryRead(accessToken);

            if (declaredExpiry is null)
            {
                LogUnreadableExpiry(FallbackLifetime.TotalMinutes);
            }

            var expiresAt = declaredExpiry ?? _timeProvider.GetUtcNow() + FallbackLifetime;

            _cached = new CachedToken(accessToken, tokens.RefreshToken, expiresAt);

            LogTokenAcquired(expiresAt);

            return accessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void InvalidateToken()
    {
        // The refresh token is dropped along with the access token. If upstream has
        // rejected one, the other is not worth trusting either.
        _cached = null;
        LogTokenInvalidated();
    }

    /// <summary>
    /// Refreshes with the stored refresh token when possible, otherwise logs in.
    /// </summary>
    private async Task<AuthTokens> AcquireTokensAsync(CancellationToken cancellationToken)
    {
        var refreshToken = _cached?.RefreshToken;

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            try
            {
                LogRefreshing();

                return await PostAsync(
                    _settings.RefreshToken!,
                    new RefreshTokenRequest(refreshToken),
                    cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                // The refresh token expires too, and independently of the access
                // token. A failure here is expected rather than exceptional, so
                // fall back to a full login instead of surfacing it.
                LogRefreshFailed(exception.Message);
            }
        }

        LogLoggingIn(_settings.Username!);

        return await PostAsync(
            _settings.Auth!,
            new LoginRequest(_settings.Username!, _settings.Password!),
            cancellationToken);
    }

    private async Task<AuthTokens> PostAsync<TRequest>(
        string path,
        TRequest body,
        CancellationToken cancellationToken)
    {
        // Created per call, not held as a field. See the note on captive
        // dependencies in the class remarks. Not disposed: the factory owns the
        // handler, and disposing the wrapper achieves nothing.
        var client = _httpClientFactory.CreateClient(HttpClientName);

        using var response = await client.PostAsJsonAsync(path, body, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            // Distinguished from a transient fault: retrying invalid credentials
            // will never succeed, so this must not look like an upstream blip.
            throw new AuthenticationFailedException(
                $"The upstream service rejected the credentials for '{_settings.Username}'.");
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AuthTokens>(cancellationToken)
               ?? throw new AuthenticationFailedException(
                   "The upstream service returned an empty authentication response.");
    }

    private bool TryGetValidToken([NotNullWhen(true)] out string? token)
    {
        var cached = _cached;

        if (cached is not null && _timeProvider.GetUtcNow() < cached.ExpiresAt - ExpiryMargin)
        {
            token = cached.AccessToken;
            return true;
        }

        token = null;
        return false;
    }

    /// <summary>
    /// Releases the refresh gate. The DI container calls this when the application
    /// shuts down, since this service is registered as a singleton. The injected
    /// HttpClient is deliberately not disposed -- it belongs to
    /// <see cref="IHttpClientFactory"/>.
    /// </summary>
    public void Dispose() => _gate.Dispose();

    private sealed record CachedToken(string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAt);

    private sealed record LoginRequest(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("password")] string Password);

    // camelCase, unlike the snake_case the same API returns.
    private sealed record RefreshTokenRequest(
        [property: JsonPropertyName("refreshToken")] string RefreshToken);

    [LoggerMessage(Level = LogLevel.Information, Message = "Acquired access token, expires at {ExpiresAt}")]
    private partial void LogTokenAcquired(DateTimeOffset expiresAt);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Refreshing access token with the stored refresh token")]
    private partial void LogRefreshing();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cached token discarded")]
    private partial void LogTokenInvalidated();

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Token refresh failed, falling back to a full login: {Reason}")]
    private partial void LogRefreshFailed(string reason);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Authenticating as {Username}")]
    private partial void LogLoggingIn(string username);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Access token carries no readable exp claim; assuming a {Minutes} minute lifetime")]
    private partial void LogUnreadableExpiry(double minutes);
}
