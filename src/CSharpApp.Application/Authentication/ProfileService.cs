namespace CSharpApp.Application.Authentication;

/// <summary>
/// Typed client over <c>/auth/profile</c>.
/// </summary>
/// <remarks>
/// Exists mainly to prove the JWT pipeline works end to end. Upstream rejects this
/// endpoint with 401 unless a valid bearer token is attached, and -- unlike the
/// product write endpoints, which upstream does not protect at all -- that makes it
/// the only place where a missing or stale token is actually observable.
///
/// Note that this service contains no auth code whatsoever. The token is attached
/// by <c>BearerTokenHandler</c> in the client's pipeline, which is the whole point
/// of doing it there.
/// </remarks>
public sealed partial class ProfileService : IProfileService
{
    private readonly HttpClient _httpClient;
    private readonly RestApiSettings _settings;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        HttpClient httpClient,
        IOptions<RestApiSettings> settings,
        ILogger<ProfileService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<UserProfile?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var profile = await _httpClient.GetFromJsonAsync<UserProfile>(
            _settings.Profile!, cancellationToken);

        LogProfileRetrieved(profile?.Email ?? "unknown");

        return profile;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved profile for {Email}")]
    private partial void LogProfileRetrieved(string email);
}
