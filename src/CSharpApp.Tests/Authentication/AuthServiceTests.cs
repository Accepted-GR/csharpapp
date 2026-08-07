namespace CSharpApp.Tests.Authentication;

public sealed class AuthServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAccessTokenAsync_AuthenticatesOnce_ForRepeatedCalls()
    {
        // The behaviour manual testing showed as "1 login for 5 requests". Asserted
        // here so a future change that drops the cache fails the build.
        using var fixture = Fixture.Returning(TokensExpiringIn(TimeSpan.FromHours(1)));

        for (var i = 0; i < 5; i++)
        {
            await fixture.Service.GetAccessTokenAsync();
        }

        Assert.Equal(1, fixture.Handler.CallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_AuthenticatesOnce_ForConcurrentCallers()
    {
        // Twenty callers arriving together on an empty cache must produce one login,
        // not twenty. This is the case the SemaphoreSlim exists for, and the one
        // that cannot be provoked reliably by hand.
        using var fixture = Fixture.Returning(TokensExpiringIn(TimeSpan.FromHours(1)));

        await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => fixture.Service.GetAccessTokenAsync()));

        Assert.Equal(1, fixture.Handler.CallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReAuthenticates_OnceTheTokenExpires()
    {
        using var fixture = Fixture.Returning(TokensExpiringIn(TimeSpan.FromMinutes(30)));

        await fixture.Service.GetAccessTokenAsync();

        fixture.Time.Advance(TimeSpan.FromMinutes(31));

        await fixture.Service.GetAccessTokenAsync();

        Assert.Equal(2, fixture.Handler.CallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_TreatsATokenInsideTheSkewMargin_AsExpired()
    {
        // A token valid for another 60 seconds is not safe to use: the two minute
        // margin covers clock skew and the flight time of the request it is about
        // to be attached to.
        using var fixture = Fixture.Returning(TokensExpiringIn(TimeSpan.FromMinutes(10)));

        await fixture.Service.GetAccessTokenAsync();

        fixture.Time.Advance(TimeSpan.FromMinutes(9));

        await fixture.Service.GetAccessTokenAsync();

        Assert.Equal(2, fixture.Handler.CallCount);
    }

    [Fact]
    public async Task GetAccessTokenAsync_RefreshesBeforeFallingBackToLogin()
    {
        using var fixture = Fixture.Returning(TokensExpiringIn(TimeSpan.FromMinutes(10)));

        await fixture.Service.GetAccessTokenAsync();
        fixture.Time.Advance(TimeSpan.FromMinutes(11));
        await fixture.Service.GetAccessTokenAsync();

        Assert.Equal("auth/login", PathOf(fixture.Handler.Requests[0]));
        Assert.Equal("auth/refresh-token", PathOf(fixture.Handler.Requests[1]));

        // The refresh token from the first response must be the one sent, otherwise
        // the refresh would succeed by accident rather than by design.
        Assert.Contains("refresh-0", fixture.Handler.Requests[1].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAccessTokenAsync_FallsBackToLogin_WhenRefreshFails()
    {
        // The refresh token expires independently of the access token, so a failed
        // refresh is an expected outcome rather than an error.
        var handler = StubHttpMessageHandler.Responding((request, index) =>
            index switch
            {
                0 => StubHttpMessageHandler.Json(HttpStatusCode.Created, TokenJson(0, TimeSpan.FromMinutes(10))),
                1 => new HttpResponseMessage(HttpStatusCode.InternalServerError),
                _ => StubHttpMessageHandler.Json(HttpStatusCode.Created, TokenJson(2, TimeSpan.FromHours(1))),
            });

        using var fixture = Fixture.Using(handler);

        await fixture.Service.GetAccessTokenAsync();
        fixture.Time.Advance(TimeSpan.FromMinutes(11));

        var token = await fixture.Service.GetAccessTokenAsync();

        Assert.Equal(3, fixture.Handler.CallCount);
        Assert.Equal("auth/refresh-token", PathOf(fixture.Handler.Requests[1]));
        Assert.Equal("auth/login", PathOf(fixture.Handler.Requests[2]));
        Assert.NotNull(token);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetAccessTokenAsync_Throws_WhenCredentialsAreRejected(HttpStatusCode status)
    {
        // Must not be an HttpRequestException: rejected credentials will not start
        // working on a retry, so they must not look like a transient upstream fault
        // to the resilience pipeline.
        using var fixture = Fixture.Using(StubHttpMessageHandler.Always(status));

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => fixture.Service.GetAccessTokenAsync());
    }

    [Fact]
    public async Task GetAccessTokenAsync_Throws_WhenTheResponseHasNoAccessToken()
    {
        using var fixture = Fixture.Using(
            StubHttpMessageHandler.Always(HttpStatusCode.Created, "{\"refresh_token\":\"only-this\"}"));

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => fixture.Service.GetAccessTokenAsync());
    }

    [Fact]
    public async Task GetAccessTokenAsync_AssumesAShortLifetime_WhenExpiryIsUnreadable()
    {
        // An opaque token must not be cached indefinitely. Re-authenticating too
        // often is a far smaller problem than trusting an expired token.
        using var fixture = Fixture.Using(StubHttpMessageHandler.Always(
            HttpStatusCode.Created, "{\"access_token\":\"opaque\",\"refresh_token\":\"r\"}"));

        await fixture.Service.GetAccessTokenAsync();

        // The usable window is the 5 minute fallback lifetime less the 2 minute
        // skew margin, so the token is good for under 3 minutes -- not 5.
        fixture.Time.Advance(TimeSpan.FromMinutes(2));
        await fixture.Service.GetAccessTokenAsync();
        Assert.Equal(1, fixture.Handler.CallCount);

        fixture.Time.Advance(TimeSpan.FromMinutes(2));
        await fixture.Service.GetAccessTokenAsync();
        Assert.Equal(2, fixture.Handler.CallCount);
    }

    [Fact]
    public async Task InvalidateToken_ForcesTheNextCallToReAuthenticate()
    {
        using var fixture = Fixture.Returning(TokensExpiringIn(TimeSpan.FromHours(1)));

        await fixture.Service.GetAccessTokenAsync();
        fixture.Service.InvalidateToken();
        await fixture.Service.GetAccessTokenAsync();

        Assert.Equal(2, fixture.Handler.CallCount);

        // The refresh token is discarded too. If upstream rejected one, the other is
        // not worth trusting, so the retry must be a full login.
        Assert.Equal("auth/login", PathOf(fixture.Handler.Requests[1]));
    }

    private static string PathOf(StubHttpMessageHandler.RecordedRequest request) =>
        request.Uri!.AbsolutePath.TrimStart('/').Replace("api/v1/", string.Empty, StringComparison.Ordinal);

    private static string TokenJson(int sequence, TimeSpan validFor) =>
        $$"""
          {"access_token":"{{TokenBuilder.ExpiringAt(Now + validFor)}}","refresh_token":"refresh-{{sequence}}"}
          """;

    private static StubHttpMessageHandler TokensExpiringIn(TimeSpan validFor) =>
        StubHttpMessageHandler.Responding((_, index) =>
            StubHttpMessageHandler.Json(HttpStatusCode.Created, TokenJson(index, validFor)));

    /// <summary>
    /// Wires an <see cref="AuthService"/> onto a stub handler and a controllable clock.
    /// </summary>
    private sealed class Fixture : IDisposable
    {
        private readonly HttpClient _client;

        private Fixture(StubHttpMessageHandler handler)
        {
            Handler = handler;
            Time = new FakeTimeProvider(Now);

            _client = new HttpClient(handler) { BaseAddress = new Uri("https://upstream.test/api/v1/") };

            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient(AuthService.HttpClientName).Returns(_client);

            Service = new AuthService(
                factory,
                Options.Create(new RestApiSettings
                {
                    BaseUrl = "https://upstream.test/api/v1/",
                    Products = "products",
                    Categories = "categories",
                    Auth = "auth/login",
                    RefreshToken = "auth/refresh-token",
                    Profile = "auth/profile",
                    Username = "test@example.com",
                    Password = "secret",
                }),
                Time,
                NullLogger<AuthService>.Instance);
        }

        public StubHttpMessageHandler Handler { get; }

        public FakeTimeProvider Time { get; }

        public AuthService Service { get; }

        public static Fixture Using(StubHttpMessageHandler handler) => new(handler);

        public static Fixture Returning(StubHttpMessageHandler handler) => new(handler);

        public void Dispose()
        {
            Service.Dispose();
            _client.Dispose();
        }
    }
}
