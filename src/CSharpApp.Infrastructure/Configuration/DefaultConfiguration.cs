namespace CSharpApp.Infrastructure.Configuration;

public static class DefaultConfiguration
{
    /// <summary>
    /// Binds and validates application settings.
    /// </summary>
    /// <remarks>
    /// <see cref="IConfiguration"/> is taken as a parameter rather than resolved
    /// through <c>services.BuildServiceProvider()</c>. Building a provider during
    /// registration creates a second, throwaway container whose singletons are
    /// distinct from the ones the application ends up using, and which is never
    /// disposed. See finding #5 in docs/AUDIT.md.
    /// </remarks>
    public static IServiceCollection AddDefaultConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ValidateOnStart turns a malformed setting into a startup failure with a
        // precise message, instead of a confusing 404 or NullReferenceException on
        // the first request that happens to use it.
        services.AddOptions<RestApiSettings>()
            .Bind(configuration.GetSection(nameof(RestApiSettings)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<HttpClientSettings>()
            .Bind(configuration.GetSection(nameof(HttpClientSettings)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
