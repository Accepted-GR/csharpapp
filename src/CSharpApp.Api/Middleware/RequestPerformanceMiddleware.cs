namespace CSharpApp.Api.Middleware;

/// <summary>
/// Measures and logs the duration of every request.
/// </summary>
/// <remarks>
/// Implements README feature #4.
///
/// Registered first in the pipeline so the measurement covers everything that
/// follows -- routing, model binding, the endpoint, serialisation and the exception
/// handler. Placing it after the exception handler would silently exclude the cost
/// of handling a failure, which is exactly the case worth measuring.
/// </remarks>
public sealed partial class RequestPerformanceMiddleware : IMiddleware
{
    private readonly PerformanceSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RequestPerformanceMiddleware> _logger;

    public RequestPerformanceMiddleware(
        IOptions<PerformanceSettings> settings,
        TimeProvider timeProvider,
        ILogger<RequestPerformanceMiddleware> logger)
    {
        _settings = settings.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = ResolveCorrelationId(context);

        // GetTimestamp rather than DateTimeOffset arithmetic: it is monotonic, so a
        // clock adjustment mid-request cannot produce a negative or wildly wrong
        // duration. Going through TimeProvider keeps it testable.
        var start = _timeProvider.GetTimestamp();

        // Headers must be written before the response body starts, which has already
        // happened by the time the finally block below runs.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[_settings.CorrelationIdHeader] = correlationId;

            if (_settings.IncludeResponseTimeHeader)
            {
                var elapsed = _timeProvider.GetElapsedTime(start).TotalMilliseconds;
                context.Response.Headers["X-Response-Time-Ms"] = elapsed.ToString("F1", CultureInfo.InvariantCulture);
            }

            return Task.CompletedTask;
        });

        // Pushed onto the log context so every log written during this request --
        // from any component, not just this middleware -- carries the correlation id.
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            try
            {
                await next(context);
            }
            finally
            {
                // In a finally block so a request that throws is still measured. An
                // unhandled failure is the most interesting timing of all.
                Log(context, _timeProvider.GetElapsedTime(start).TotalMilliseconds);
            }
        }
    }

    private void Log(HttpContext context, double elapsedMs)
    {
        var method = context.Request.Method;
        var status = context.Response.StatusCode;

        // The route pattern, not the raw path: "/api/v1/products/{id}" aggregates
        // across requests where "/api/v1/products/42" would produce a distinct
        // timing series per id and make the data useless.
        var route = context.GetEndpoint() is RouteEndpoint endpoint
            ? endpoint.RoutePattern.RawText ?? context.Request.Path.Value ?? "unknown"
            : context.Request.Path.Value ?? "unknown";

        if (elapsedMs >= _settings.SlowRequestThresholdMs)
        {
            LogSlowRequest(method, route, status, elapsedMs, _settings.SlowRequestThresholdMs);
        }
        else
        {
            LogRequest(method, route, status, elapsedMs);
        }
    }

    /// <summary>
    /// Reuses an inbound correlation id when present, otherwise creates one.
    /// </summary>
    /// <remarks>
    /// Honouring an inbound value is what allows a single request to be traced
    /// across service boundaries rather than only within this one.
    /// </remarks>
    private string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_settings.CorrelationIdHeader, out var supplied) &&
            !string.IsNullOrWhiteSpace(supplied))
        {
            return supplied.ToString();
        }

        // The trace identifier is already unique per request and is what ProblemDetails
        // reports, so reusing it keeps the logs and error responses joinable.
        return context.TraceIdentifier;
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "{Method} {Route} responded {StatusCode} in {ElapsedMs:F1}ms")]
    private partial void LogRequest(string method, string route, int statusCode, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "{Method} {Route} responded {StatusCode} in {ElapsedMs:F1}ms, " +
                  "exceeding the {ThresholdMs}ms threshold")]
    private partial void LogSlowRequest(
        string method, string route, int statusCode, double elapsedMs, int thresholdMs);
}
