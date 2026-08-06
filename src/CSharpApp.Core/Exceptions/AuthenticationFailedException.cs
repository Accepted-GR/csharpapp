namespace CSharpApp.Core.Exceptions;

/// <summary>
/// Thrown when the upstream service will not issue an access token.
/// </summary>
/// <remarks>
/// Distinct from the transient <see cref="HttpRequestException"/> on purpose.
/// Rejected credentials will not start working on a retry, so this must not be
/// mistaken for an upstream blip -- it maps to 502 rather than being retried by
/// the resilience pipeline.
/// </remarks>
public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException()
        : base("Authentication against the upstream service failed.")
    {
    }

    public AuthenticationFailedException(string message)
        : base(message)
    {
    }

    public AuthenticationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
