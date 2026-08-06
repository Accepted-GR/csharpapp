namespace CSharpApp.Core.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Returns a valid access token, acquiring or refreshing one if necessary.
    /// </summary>
    /// <remarks>
    /// Callers should treat this as cheap and call it per request rather than
    /// caching the result themselves -- the caching, expiry tracking and refresh
    /// decision all live behind this method.
    /// </remarks>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discards the cached token so the next call acquires a fresh one.
    /// </summary>
    /// <remarks>
    /// Used when the upstream service rejects a token we believed was still valid,
    /// which means our cached copy is stale rather than merely expired.
    /// </remarks>
    void InvalidateToken();
}
