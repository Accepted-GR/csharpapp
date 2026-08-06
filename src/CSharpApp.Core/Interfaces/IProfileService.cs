namespace CSharpApp.Core.Interfaces;

/// <summary>
/// Reads the authenticated user's profile from the upstream service.
/// </summary>
/// <remarks>
/// Separate from <see cref="IAuthService"/> on purpose. Auth is the token
/// *producer* and must talk to upstream unauthenticated; this is a token
/// *consumer* whose client carries the bearer handler. Collapsing the two would
/// mean one client both needing and providing the token.
/// </remarks>
public interface IProfileService
{
    Task<UserProfile?> GetProfileAsync(CancellationToken cancellationToken = default);
}
