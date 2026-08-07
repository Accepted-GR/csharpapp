namespace CSharpApp.Application.Common;

/// <summary>
/// Shared handling of upstream responses.
/// </summary>
/// <remarks>
/// Factored out because products and categories need identical treatment, and the
/// not-found translation is subtle enough that two copies would drift.
/// </remarks>
internal static class UpstreamResponse
{
    /// <summary>
    /// Translates the upstream not-found response into
    /// <see cref="UpstreamNotFoundException"/>.
    /// </summary>
    /// <remarks>
    /// The upstream API answers a missing entity with <c>400 Bad Request</c> and an
    /// <c>EntityNotFoundError</c> body rather than a 404. Passing that through would
    /// tell our callers their request was malformed when they simply asked for
    /// something that is not there.
    ///
    /// The body is inspected rather than treating every 400 as not-found, so a
    /// genuinely malformed request is still reported as one.
    /// </remarks>
    public static async Task EnsureFoundAsync(
        HttpResponseMessage response,
        string entity,
        int id,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.NotFound))
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound ||
            body.Contains("EntityNotFoundError", StringComparison.OrdinalIgnoreCase))
        {
            throw UpstreamNotFoundException.For(entity, id);
        }
    }

    /// <summary>
    /// Reads a successful response body, treating a null payload as a fault.
    /// </summary>
    public static async Task<T> ReadAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
               ?? throw new JsonException(
                   $"The upstream service returned an empty {typeof(T).Name} payload.");
    }
}
