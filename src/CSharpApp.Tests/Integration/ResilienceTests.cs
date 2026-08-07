namespace CSharpApp.Tests.Integration;

/// <summary>
/// The resilience pipeline configured in Phase 1.
/// </summary>
/// <remarks>
/// The circuit breaker was explicitly listed as unverified in the Phase 1 and
/// Phase 2 pull requests: opening it requires ten failures inside the sampling
/// window, and driving that against a live upstream took roughly eight seconds per
/// attempt and killed the test host before the threshold was reached. Against a
/// stub it is immediate and deterministic.
/// </remarks>
public sealed class ResilienceTests
{
    [Fact]
    public async Task Retry_RecoversFromATransientFailure()
    {
        // Fails the first product call, succeeds on the retry. A single 200 proves
        // the retry strategy is actually attached, not merely configured.
        var productCalls = 0;

        using var factory = new ApiFactory((request, _) =>
        {
            if (ApiFactory.IsAuthCall(request))
            {
                return StubHttpMessageHandler.Json(HttpStatusCode.Created, AuthToken);
            }

            return Interlocked.Increment(ref productCalls) == 1
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : StubHttpMessageHandler.Json(HttpStatusCode.OK, "[]");
        });

        using var client = WithFastRetries(factory).CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/products", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(productCalls >= 2, $"Expected a retry, saw {productCalls} upstream calls.");
    }

    [Fact]
    public async Task CircuitBreaker_OpensAfterSustainedFailure_AndReportsServiceUnavailable()
    {
        // Sustained upstream failure should stop being retried and start failing
        // fast, so the caller sees 503 rather than the 502 an individual failure
        // produces.
        using var factory = new ApiFactory((request, _) =>
            ApiFactory.IsAuthCall(request)
                ? StubHttpMessageHandler.Json(HttpStatusCode.Created, AuthToken)
                : new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var client = WithFastRetries(factory).CreateClient();

        var statuses = new List<HttpStatusCode>();

        for (var i = 0; i < 20; i++)
        {
            var response = await client.GetAsync(new Uri("/api/v1/products", UriKind.Relative));
            statuses.Add(response.StatusCode);
        }

        Assert.Contains(HttpStatusCode.ServiceUnavailable, statuses);

        // Every failure is still a 5xx: an upstream outage must never be reported
        // as a client error.
        Assert.All(statuses, status => Assert.True((int)status >= 500, $"Unexpected status {status}."));
    }

    [Fact]
    public async Task UpstreamFailure_IsReportedAs502_BeforeTheBreakerOpens()
    {
        using var factory = new ApiFactory((request, _) =>
            ApiFactory.IsAuthCall(request)
                ? StubHttpMessageHandler.Json(HttpStatusCode.Created, AuthToken)
                : new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var client = WithFastRetries(factory).CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/products", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    /// <summary>
    /// Removes retry backoff so the breaker can be driven without real delays.
    /// </summary>
    private static WebApplicationFactory<Program> WithFastRetries(ApiFactory factory) =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["HttpClientSettings:RetryCount"] = "1",
                    ["HttpClientSettings:SleepDuration"] = "1",
                    ["HttpClientSettings:TimeoutSeconds"] = "5",
                })));

    private static string AuthToken =>
        $$"""
          {"access_token":"{{TokenBuilder.ExpiringAt(DateTimeOffset.UtcNow.AddHours(1))}}","refresh_token":"r"}
          """;
}
