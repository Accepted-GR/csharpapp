namespace CSharpApp.Api.Middleware;

/// <summary>
/// Translates unhandled exceptions into RFC 9457 ProblemDetails responses.
/// </summary>
/// <remarks>
/// AddProblemDetails() was already registered but no handler was ever wired into
/// the pipeline, so nothing converted an exception into a ProblemDetails payload
/// and failures surfaced as a bare 500 with a full stack trace. See finding #6 in
/// docs/AUDIT.md.
///
/// The mapping matters for the upstream calls introduced in this phase: a
/// timeout, an open circuit breaker or a cancelled request are not server faults
/// and should not be reported as 500.
/// </remarks>
public sealed partial class UpstreamExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<UpstreamExceptionHandler> _logger;

    public UpstreamExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<UpstreamExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Converted once and reused. PathString -> string allocates, so doing it
        // inline at each logging call site would repeat the work and trips CA1873.
        var path = httpContext.Request.Path.Value ?? string.Empty;

        // A client that disconnects mid-request is not an error worth reporting,
        // and there is no longer a connection to write a response to.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            LogRequestAborted(path);
            return false;
        }

        var (statusCode, title, isRecognised) = Map(exception);

        // Branch on whether the exception was recognised, not on the status code.
        // Every mapped upstream failure is itself a 5xx (502/503/504), so a
        // "statusCode >= 500" test would classify all of them as unhandled and
        // make a routine upstream blip look like a bug in this service.
        if (isRecognised)
        {
            LogUpstreamFailure(path, title, exception.Message);
        }
        else
        {
            LogUnhandled(exception, path);
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                // The exception message is deliberately withheld outside
                // Development so internal detail is not leaked to callers.
                Detail = httpContext.RequestServices
                    .GetRequiredService<IHostEnvironment>()
                    .IsDevelopment()
                    ? exception.Message
                    : null,
                Instance = path,
            },
        });
    }

    /// <summary>
    /// Maps an exception onto a status code, and reports whether it was recognised
    /// at all. The flag is what lets the caller tell an expected upstream failure
    /// apart from a genuine defect, since both produce a 5xx.
    /// </summary>
    private static (int StatusCode, string Title, bool IsRecognised) Map(Exception exception) => exception switch
    {
        // The circuit breaker is open: upstream is known to be failing, so reject
        // immediately rather than adding load to it.
        BrokenCircuitException => (StatusCodes.Status503ServiceUnavailable,
            "The upstream service is currently unavailable.", true),

        TimeoutRejectedException or TaskCanceledException => (StatusCodes.Status504GatewayTimeout,
            "The upstream service did not respond in time.", true),

        // Raised by GetFromJsonAsync when the upstream returns a failure status.
        HttpRequestException => (StatusCodes.Status502BadGateway,
            "The upstream service returned an unexpected response.", true),

        // A malformed upstream payload is an integration fault, not a server bug.
        JsonException => (StatusCodes.Status502BadGateway,
            "The upstream service returned a malformed response.", true),

        // Our credentials were rejected. A configuration problem on our side, but
        // from the caller's perspective the upstream dependency is unusable.
        AuthenticationFailedException => (StatusCodes.Status502BadGateway,
            "Could not authenticate against the upstream service.", true),

        // Upstream reports a missing entity as 400 with an EntityNotFoundError
        // body. The service layer translates it; this reports it honestly.
        UpstreamNotFoundException => (StatusCodes.Status404NotFound,
            "The requested resource was not found.", true),

        _ => (StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.", false),
    };

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception processing {Path}")]
    private partial void LogUnhandled(Exception exception, string path);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Upstream failure processing {Path}: {Title} ({Reason})")]
    private partial void LogUpstreamFailure(string path, string title, string reason);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Request to {Path} was aborted by the client")]
    private partial void LogRequestAborted(string path);
}
