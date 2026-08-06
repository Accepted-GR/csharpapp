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
    /// Returns the profile of the authenticated user.
    /// </summary>
    Task<UserProfile?> GetProfileAsync(CancellationToken cancellationToken = default);
}
