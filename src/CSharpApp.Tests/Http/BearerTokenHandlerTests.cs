namespace CSharpApp.Tests.Http;

public sealed class BearerTokenHandlerTests
{
    [Fact]
    public async Task SendAsync_AttachesTheBearerToken()
    {
        using var fixture = Fixture.Returning(StubHttpMessageHandler.Always(HttpStatusCode.OK));

        await fixture.Client.GetAsync(new Uri("https://upstream.test/products"));

        Assert.Equal("Bearer token-1", fixture.Handler.Requests[0].Authorization);
    }

    [Fact]
    public async Task SendAsync_DoesNotOverwriteAnExplicitAuthorizationHeader()
    {
        // A caller that sets its own header has a reason to; the ambient token must
        // not silently replace it.
        using var fixture = Fixture.Returning(StubHttpMessageHandler.Always(HttpStatusCode.OK));

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://upstream.test/products"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "caller-supplied");

        await fixture.Client.SendAsync(request);

        Assert.Equal("Bearer caller-supplied", fixture.Handler.Requests[0].Authorization);
    }

    [Fact]
    public async Task SendAsync_RetriesOnceWithAFreshToken_AfterA401()
    {
        // Flagged as unverified in the Phase 2 PR because it could not be driven
        // against a live upstream. A stub makes it a two-line setup.
        var handler = StubHttpMessageHandler.Responding((_, index) =>
            new HttpResponseMessage(index == 0 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK));

        using var fixture = Fixture.Returning(handler);

        var response = await fixture.Client.GetAsync(new Uri("https://upstream.test/products"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.CallCount);

        // The stale token is discarded and a new one acquired, so the replay must
        // not carry the token that was just rejected.
        Assert.Equal("Bearer token-1", handler.Requests[0].Authorization);
        Assert.Equal("Bearer token-2", handler.Requests[1].Authorization);
    }

    [Fact]
    public async Task SendAsync_RetriesOnlyOnce_WhenTheSecondAttemptAlsoFails()
    {
        // Without this bound a permanently rejecting upstream would loop forever.
        var handler = StubHttpMessageHandler.Always(HttpStatusCode.Unauthorized);

        using var fixture = Fixture.Returning(handler);

        var response = await fixture.Client.GetAsync(new Uri("https://upstream.test/products"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task SendAsync_PreservesTheRequestBody_AcrossA401Replay()
    {
        // The reason the body is buffered before the first send. An
        // HttpRequestMessage cannot be sent twice and its content stream may
        // already be consumed, so without buffering the replay would lose the body
        // on every POST and PUT.
        var handler = StubHttpMessageHandler.Responding((_, index) =>
            new HttpResponseMessage(index == 0 ? HttpStatusCode.Unauthorized : HttpStatusCode.Created));

        using var fixture = Fixture.Returning(handler);

        var payload = new ProductWriteModel
        {
            Title = "replayed",
            Price = 10m,
            Description = "body must survive",
            CategoryId = 1,
            Images = ["https://example.test/a.png"],
        };

        var response = await fixture.Client.PostAsJsonAsync(new Uri("https://upstream.test/products"), payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, handler.CallCount);

        Assert.Contains("body must survive", handler.Requests[0].Body, StringComparison.Ordinal);
        Assert.Contains("body must survive", handler.Requests[1].Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_DoesNotRetry_OnASuccessfulResponse()
    {
        using var fixture = Fixture.Returning(StubHttpMessageHandler.Always(HttpStatusCode.OK));

        await fixture.Client.GetAsync(new Uri("https://upstream.test/products"));

        Assert.Equal(1, fixture.Handler.CallCount);
    }

    private sealed class Fixture : IDisposable
    {
        private Fixture(StubHttpMessageHandler handler)
        {
            Handler = handler;

            var tokenNumber = 0;
            var auth = Substitute.For<IAuthService>();

            // A new token each call, so a test can tell the replayed token apart
            // from the rejected one.
            auth.GetAccessTokenAsync(Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromResult($"token-{Interlocked.Increment(ref tokenNumber)}"));

            var bearer = new BearerTokenHandler(auth, NullLogger<BearerTokenHandler>.Instance)
            {
                InnerHandler = handler,
            };

            Client = new HttpClient(bearer);
        }

        public StubHttpMessageHandler Handler { get; }

        public HttpClient Client { get; }

        public static Fixture Returning(StubHttpMessageHandler handler) => new(handler);

        public void Dispose() => Client.Dispose();
    }
}
