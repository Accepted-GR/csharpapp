namespace CSharpApp.Core.Exceptions;

/// <summary>
/// Thrown when the upstream service has no entity matching the request.
/// </summary>
/// <remarks>
/// Needed because the upstream API does not use 404 for this. A request for a
/// product or category that does not exist returns <c>400 Bad Request</c> with an
/// <c>EntityNotFoundError</c> body -- as do PUT and DELETE against a missing id,
/// and POST referencing a category id that does not exist.
///
/// Passing that through unchanged would tell our callers they sent a malformed
/// request when in fact they asked for something that is not there. This exception
/// carries the distinction so the API can answer 404.
/// </remarks>
public sealed class UpstreamNotFoundException : Exception
{
    public UpstreamNotFoundException(string message)
        : base(message)
    {
    }

    public UpstreamNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates an exception describing a missing entity of the given type and id.
    /// </summary>
    public static UpstreamNotFoundException For(string entity, int id) =>
        new($"{entity} with id {id} was not found.");
}
