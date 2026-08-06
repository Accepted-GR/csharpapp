namespace CSharpApp.Infrastructure.Http;

/// <summary>
/// Attaches the upstream access token to outgoing requests.
/// </summary>
/// <remarks>
/// Putting this in the pipeline rather than in each service keeps every service
/// free of auth concerns: no service asks for a token, and none can forget to.
///
/// This handler must never be attached to the client that <c>AuthService</c> uses,
/// or acquiring a token would itself require a token and recurse until the stack
/// runs out. <c>HttpConfiguration</c> registers the two clients separately for
/// exactly that reason.
/// </remarks>
public sealed partial class BearerTokenHandler : DelegatingHandler
{
    private readonly IAuthService _authService;
    private readonly ILogger<BearerTokenHandler> _logger;

    public BearerTokenHandler(IAuthService authService, ILogger<BearerTokenHandler> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // An explicitly supplied header wins, so a caller can override the ambient
        // token when it needs to.
        if (request.Headers.Authorization is null)
        {
            var token = await _authService.GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // Buffered before sending, not after. Once a request has been sent its
        // content stream may already be consumed, and a request message cannot be
        // sent twice -- so the replay below would fail on any POST or PUT unless
        // the body is captured first.
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // A 401 on a token we believed was valid means our cached copy is stale --
        // revoked upstream, or the clock skew margin was not enough. Retrying once
        // with a freshly acquired token turns a spurious failure into a success.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            LogRetryingAfterUnauthorized(request.RequestUri?.PathAndQuery ?? "unknown");

            response.Dispose();

            _authService.InvalidateToken();

            using var retried = await CloneAsync(request, cancellationToken);
            retried.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer", await _authService.GetAccessTokenAsync(cancellationToken));

            return await base.SendAsync(retried, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// Copies a request so it can be sent a second time.
    /// </summary>
    /// <remarks>
    /// <see cref="HttpRequestMessage"/> cannot be reused, so a retry needs a fresh
    /// instance. The body relies on the caller having buffered the content first.
    /// </remarks>
    private static async Task<HttpRequestMessage> CloneAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy,
        };

        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var content = new ByteArrayContent(body);

            // Content-Type and friends live on the content, not the request.
            foreach (var header in request.Content.Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = content;
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Carries across anything the resilience pipeline or telemetry attached.
        foreach (var option in request.Options)
        {
            ((IDictionary<string, object?>)clone.Options)[option.Key] = option.Value;
        }

        return clone;
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Upstream returned 401 for {Path}; discarding the cached token and retrying once")]
    private partial void LogRetryingAfterUnauthorized(string path);
}
