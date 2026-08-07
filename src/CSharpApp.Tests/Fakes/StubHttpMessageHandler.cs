namespace CSharpApp.Tests.Fakes;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that returns scripted responses instead of
/// making network calls.
/// </summary>
/// <remarks>
/// The foundation of this test suite, for three reasons.
///
/// The upstream API is a mutable public demo: during manual verification a
/// product's description changed to "Prueba" and its image URL changed between two
/// consecutive runs. Any assertion against live data is a flaky test.
///
/// It makes the previously untestable testable. The circuit breaker opening and
/// the 401 retry could not be driven by hand against a live host -- opening the
/// breaker needs ten failures inside a sampling window. Here a failure is one line
/// of setup.
///
/// And it removes the network from CI entirely, so the suite is fast and offline.
/// </remarks>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _responder;
    private readonly List<RecordedRequest> _requests = [];

    private int _callCount;

    private StubHttpMessageHandler(Func<HttpRequestMessage, int, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    /// <summary>Every request received, in order, with its body captured.</summary>
    public IReadOnlyList<RecordedRequest> Requests => _requests;

    /// <summary>Number of requests that reached this handler.</summary>
    public int CallCount => _callCount;

    /// <summary>Responds using a callback, given the request and a zero-based call index.</summary>
    public static StubHttpMessageHandler Responding(
        Func<HttpRequestMessage, int, HttpResponseMessage> responder) => new(responder);

    /// <summary>Always returns the same status and body.</summary>
    public static StubHttpMessageHandler Always(HttpStatusCode status, string json = "{}") =>
        new((_, _) => Json(status, json));

    /// <summary>
    /// Returns each response in turn, repeating the last one once exhausted.
    /// </summary>
    /// <remarks>
    /// Sequencing is what makes retry and refresh behaviour testable: "fail twice,
    /// then succeed" is the exact shape those paths need.
    /// </remarks>
    public static StubHttpMessageHandler Sequence(params HttpResponseMessage[] responses) =>
        new((_, index) => Clone(responses[Math.Min(index, responses.Length - 1)]));

    public static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var index = Interlocked.Increment(ref _callCount) - 1;

        // The body is captured here rather than left on the request, because the
        // content stream may be consumed by the time a test inspects it.
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        lock (_requests)
        {
            _requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri,
                request.Headers.Authorization?.ToString(),
                body));
        }

        return _responder(request, index);
    }

    /// <summary>
    /// Copies a response so a sequence entry can be returned more than once.
    /// </summary>
    /// <remarks>
    /// An <see cref="HttpResponseMessage"/> content stream is consumed on read, so
    /// handing out the same instance twice would yield an empty body the second
    /// time.
    /// </remarks>
    private static HttpResponseMessage Clone(HttpResponseMessage source)
    {
        var clone = new HttpResponseMessage(source.StatusCode);

        if (source.Content is not null)
        {
            var body = source.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            clone.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return clone;
    }

    public sealed record RecordedRequest(
        HttpMethod Method,
        Uri? Uri,
        string? Authorization,
        string? Body);
}
