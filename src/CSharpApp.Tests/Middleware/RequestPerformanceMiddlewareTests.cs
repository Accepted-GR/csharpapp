namespace CSharpApp.Tests.Middleware;

public sealed class RequestPerformanceMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_LogsAtInformation_ForAFastRequest()
    {
        var (logger, context) = Arrange();

        await Run(logger, context, TimeSpan.FromMilliseconds(100));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
    }

    [Fact]
    public async Task InvokeAsync_LogsAtWarning_ForASlowRequest()
    {
        // The threshold is the whole point: logging every request at one level makes
        // the slow ones invisible in aggregate.
        var (logger, context) = Arrange();

        await Run(logger, context, TimeSpan.FromMilliseconds(750));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
    }

    [Fact]
    public async Task InvokeAsync_LogsAtWarning_ExactlyAtTheThreshold()
    {
        // Boundary case. "At or above" is the documented contract, so 500ms with a
        // 500ms threshold must escalate.
        var (logger, context) = Arrange();

        await Run(logger, context, TimeSpan.FromMilliseconds(500));

        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
    }

    [Fact]
    public async Task InvokeAsync_StillMeasures_WhenTheRequestThrows()
    {
        // The reason the measurement is in a finally block. An unhandled failure is
        // the timing most worth knowing about, so it must not be the one case that
        // goes unrecorded.
        var (logger, context) = Arrange();

        var middleware = Build(logger, new FakeTimeProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context, _ => throw new InvalidOperationException("boom")));

        Assert.Single(logger.Entries);
    }

    [Fact]
    public async Task InvokeAsync_LogsTheStatusCode()
    {
        var (logger, context) = Arrange();
        context.Response.StatusCode = StatusCodes.Status404NotFound;

        await Run(logger, context, TimeSpan.FromMilliseconds(10));

        Assert.Contains("404", Assert.Single(logger.Entries).Message, StringComparison.Ordinal);
    }

    // The response headers -- X-Correlation-Id and X-Response-Time-Ms -- are
    // asserted in the integration tests rather than here. They are written from an
    // OnStarting callback, and DefaultHttpContext's stub response feature discards
    // those callbacks instead of invoking them, so a unit test could only ever
    // assert that the header is absent. A real server fires them; see
    // PerformanceHeadersTests.

    private static (CapturingLoggerProvider.CapturingLogger Logger, DefaultHttpContext Context) Arrange() =>
        (new CapturingLoggerProvider.CapturingLogger(), new DefaultHttpContext());

    private static RequestPerformanceMiddleware Build(
        CapturingLoggerProvider.CapturingLogger logger,
        TimeProvider time,
        PerformanceSettings? settings = null) =>
        new(Options.Create(settings ?? new PerformanceSettings()), time, logger);

    private static async Task Run(
        CapturingLoggerProvider.CapturingLogger logger,
        DefaultHttpContext context,
        TimeSpan duration)
    {
        var time = new FakeTimeProvider();
        var middleware = Build(logger, time);

        await middleware.InvokeAsync(context, _ =>
        {
            // FakeTimeProvider drives the elapsed time, so a "slow" request costs
            // nothing to simulate and the test never actually waits.
            time.Advance(duration);
            return context.Response.StartAsync();
        });
    }
}
