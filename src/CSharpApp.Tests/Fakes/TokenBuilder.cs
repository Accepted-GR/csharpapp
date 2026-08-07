namespace CSharpApp.Tests.Fakes;

/// <summary>
/// Builds JWT-shaped strings for tests.
/// </summary>
/// <remarks>
/// The tokens are not signed and are not meant to be. Nothing in this solution
/// validates a signature -- we are the client, not the resource server -- so a
/// realistic three-segment shape with a decodable payload is exactly as much
/// fidelity as the code under test can observe.
/// </remarks>
public static class TokenBuilder
{
    /// <summary>Builds a token whose payload is the supplied raw string.</summary>
    public static string WithClaims(string payload)
    {
        var encoded = Base64UrlEncode(payload);

        return $"{Base64UrlEncode("{\"alg\":\"HS256\"}")}.{encoded}.not-a-real-signature";
    }

    /// <summary>Builds a token expiring at the given instant.</summary>
    public static string ExpiringAt(DateTimeOffset expiresAt) =>
        WithClaims($"{{\"sub\":1,\"exp\":{expiresAt.ToUnixTimeSeconds()}}}");

    private static string Base64UrlEncode(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
