namespace CSharpApp.Tests.Integration;

/// <summary>
/// Verifies the dependency graph can actually be built.
/// </summary>
/// <remarks>
/// Added because of a real failure. TimeProvider was injected into AuthService in
/// Phase 2 but never registered, which compiled cleanly and only surfaced as a 500
/// at runtime: "Unable to resolve service for type 'System.TimeProvider'".
///
/// ValidateOnBuild turns that class of mistake into a startup failure, and this
/// test turns it into a build failure.
/// </remarks>
public sealed class ContainerValidationTests
{
    [Fact]
    public void EveryRegisteredService_CanBeConstructed()
    {
        using var factory = ApiFactory.RespondingWith(HttpStatusCode.OK, "[]");

        // Forces the host to build. ValidateOnBuild walks every registration and
        // reports the ones whose dependencies cannot be satisfied.
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IProductsService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICategoriesService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAuthService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IProfileService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void AuthService_IsASingleton_SoTheTokenCacheIsShared()
    {
        // A transient AuthService would re-authenticate per resolution, defeating
        // the cache the token lifecycle depends on.
        using var factory = ApiFactory.RespondingWith(HttpStatusCode.OK, "[]");

        using var first = factory.Services.CreateScope();
        using var second = factory.Services.CreateScope();

        Assert.Same(
            first.ServiceProvider.GetRequiredService<IAuthService>(),
            second.ServiceProvider.GetRequiredService<IAuthService>());
    }

    [Fact]
    public void Settings_AreValidatedOnStart()
    {
        // A leading slash resolves against the host root and discards the base
        // path, which was audit finding #2. It must fail startup, not produce a
        // puzzling 404 on first use.
        using var factory = new ApiFactory((_, _) => new HttpResponseMessage(HttpStatusCode.OK));

        using var configured = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RestApiSettings:Categories"] = "/categories",
                })));

        var exception = Assert.ThrowsAny<Exception>(() => configured.CreateClient());

        Assert.Contains("must not start with", Flatten(exception), StringComparison.Ordinal);
    }

    private static string Flatten(Exception exception)
    {
        var messages = new List<string>();

        for (var current = exception; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        return string.Join(" | ", messages);
    }
}
