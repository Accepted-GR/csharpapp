namespace CSharpApp.Application.Authentication;

/// <summary>
/// Reads the <c>exp</c> claim out of a JWT so the cache knows when its token
/// stops being usable.
/// </summary>
/// <remarks>
/// This deliberately does not validate the token, and no JWT library is pulled in
/// to do so. We are the client here, not the resource server: the signing key is
/// the upstream service's and validation is its responsibility. All we need is one
/// unauthenticated claim to decide when to refresh, and assuming a fixed lifetime
/// instead would silently break the moment upstream changed it.
///
/// The upstream access token currently carries a 20 day expiry and the refresh
/// token 10 hours -- which is the reverse of the usual arrangement, and a good
/// reason to read the value rather than hard-code an assumption about it.
/// </remarks>
internal static class JwtExpiry
{
    /// <summary>
    /// Returns the token's expiry, or <c>null</c> if it cannot be determined.
    /// </summary>
    public static DateTimeOffset? TryRead(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        // header.payload.signature
        var parts = accessToken.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        try
        {
            var payload = Base64Url.DecodeFromChars(parts[1]);

            using var document = JsonDocument.Parse(payload);

            if (document.RootElement.TryGetProperty("exp", out var exp) &&
                exp.TryGetInt64(out var secondsSinceEpoch))
            {
                return DateTimeOffset.FromUnixTimeSeconds(secondsSinceEpoch);
            }
        }
        catch (FormatException)
        {
            // Not valid base64url; treated as an unknown expiry below.
        }
        catch (JsonException)
        {
            // Payload was not JSON; likewise.
        }

        return null;
    }
}
