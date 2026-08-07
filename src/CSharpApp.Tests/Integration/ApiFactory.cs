namespace CSharpApp.Tests.Integration;

/// <summary>
/// Boots the real application with the network replaced by a stub.
/// </summary>
/// <remarks>
/// Everything else is genuine: the DI graph, the middleware pipeline, routing,
/// model binding, the validation filters, the exception handler and ProblemDetails.
/// Only the outbound HTTP transport is swapped, which is the seam that makes the
/// tests deterministic and offline while still exercising the wiring that unit
/// tests cannot reach.
/// </remarks>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly StubHttpMessageHandler _upstream;

    public ApiFactory(Func<HttpRequestMessage, int, HttpResponseMessage> responder)
    {
        _upstream = StubHttpMessageHandler.Responding(responder);
    }

    /// <summary>Requests that reached the stubbed upstream service.</summary>
    public StubHttpMessageHandler Upstream => _upstream;

    /// <summary>
    /// Responds to any auth call with a token and to everything else with the
    /// supplied body.
    /// </summary>
    /// <remarks>
    /// Convenience for the common case: almost every endpoint sits behind the bearer
    /// handler, so a test that forgot to script the token exchange would fail for a
    /// reason unrelated to what it is testing.
    /// </remarks>
    public static ApiFactory RespondingWith(HttpStatusCode status, string json) =>
        new((request, _) => IsAuthCall(request)
            ? StubHttpMessageHandler.Json(HttpStatusCode.Created, TokenResponse)
            : StubHttpMessageHandler.Json(status, json));

    public static bool IsAuthCall(HttpRequestMessage request) =>
        request.RequestUri?.AbsolutePath.Contains("/auth/", StringComparison.Ordinal) == true;

    private static string TokenResponse =>
        $$"""
          {"access_token":"{{TokenBuilder.ExpiringAt(DateTimeOffset.UtcNow.AddHours(1))}}","refresh_token":"r"}
          """;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            // Supplied explicitly so the tests do not depend on whatever happens to
            // be in the developer's user-secrets store.
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RestApiSettings:BaseUrl"] = "https://upstream.test/api/v1/",
                ["RestApiSettings:Username"] = "test@example.com",
                ["RestApiSettings:Password"] = "secret",
                ["PerformanceSettings:SlowRequestThresholdMs"] = "500",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replaces the transport underneath every named and typed client at
            // once, so the resilience pipeline and bearer handler above it stay in
            // place and are genuinely exercised.
            services.ConfigureAll<HttpClientFactoryOptions>(options =>
                options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
                    handlerBuilder.PrimaryHandler = _upstream));
        });
    }
}
