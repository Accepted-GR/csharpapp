namespace CSharpApp.Infrastructure.Configuration;

public static class HttpConfiguration
{
    /// <summary>
    /// Registers the upstream API clients as typed clients backed by
    /// <see cref="IHttpClientFactory"/>, with a shared resilience pipeline.
    /// </summary>
    /// <remarks>
    /// This method was previously an empty no-op (finding #3). The dependencies it
    /// needs were already declared in the project file, so the intent was clear --
    /// only the implementation was missing.
    ///
    /// Using the factory is what fixes finding #1. The original code newed up an
    /// <see cref="HttpClient"/> inside a singleton and assigned
    /// <see cref="HttpClient.BaseAddress"/> on every call, which throws once a
    /// request has been sent. Configuring the address once at registration time
    /// removes the mutation entirely, and the factory pools and rotates handlers so
    /// neither socket exhaustion nor stale DNS is a concern.
    /// </remarks>
    public static IServiceCollection AddHttpConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var restApi = configuration.GetSection(nameof(RestApiSettings)).Get<RestApiSettings>()
                      ?? throw new InvalidOperationException(
                          $"Configuration section '{nameof(RestApiSettings)}' is missing.");

        var httpSettings = configuration.GetSection(nameof(HttpClientSettings)).Get<HttpClientSettings>()
                           ?? new HttpClientSettings();

        services.AddHttpClient<IProductsService, ProductsService>(client =>
            {
                client.BaseAddress = BuildBaseAddress(restApi.BaseUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(httpSettings.LifeTime))
            .AddResilience(httpSettings);

        return services;
    }

    /// <summary>
    /// Applies retry, circuit breaker and per-attempt timeout strategies.
    /// </summary>
    /// <remarks>
    /// Order matters. The pipeline executes outermost first, so the total timeout
    /// wraps the retry loop, which wraps the circuit breaker, which wraps the
    /// per-attempt timeout. That way a single slow attempt is cut short and
    /// retried, while the overall operation still has a hard ceiling.
    /// </remarks>
    private static IHttpClientBuilder AddResilience(
        this IHttpClientBuilder builder,
        HttpClientSettings settings)
    {
        builder.AddResilienceHandler("upstream-api", pipeline =>
        {
            var attemptTimeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);

            pipeline.AddTimeout(new HttpTimeoutStrategyOptions
            {
                Name = "total-timeout",
                // Generous enough to accommodate every retry attempt plus backoff.
                Timeout = attemptTimeout * (settings.RetryCount + 1) + TimeSpan.FromSeconds(5),
            });

            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                Name = "upstream-retry",
                MaxRetryAttempts = settings.RetryCount,
                Delay = TimeSpan.FromMilliseconds(settings.SleepDuration),
                // Exponential growth with jitter, so concurrent callers that fail
                // together do not synchronise their retries into a thundering herd.
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = static args => ValueTask.FromResult(
                    HttpClientResiliencePredicates.IsTransient(args.Outcome)),
            });

            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                Name = "upstream-circuit-breaker",
                // Trip once half the calls in the sampling window fail, so a sustained
                // upstream outage fails fast instead of queueing doomed retries.
                FailureRatio = 0.5,
                MinimumThroughput = 10,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(15),
            });

            pipeline.AddTimeout(new HttpTimeoutStrategyOptions
            {
                Name = "attempt-timeout",
                Timeout = attemptTimeout,
            });
        });

        return builder;
    }

    /// <summary>
    /// Ensures the base address ends with a trailing slash.
    /// </summary>
    /// <remarks>
    /// Without one, <see cref="Uri"/> resolution discards the final path segment:
    /// ".../api/v1" + "products" resolves to ".../api/products". Normalising here
    /// means a missing slash in configuration cannot produce a puzzling 404.
    /// </remarks>
    private static Uri BuildBaseAddress(string? baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        return new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + '/', UriKind.Absolute);
    }
}
