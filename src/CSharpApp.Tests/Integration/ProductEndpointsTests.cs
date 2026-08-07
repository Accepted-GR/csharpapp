namespace CSharpApp.Tests.Integration;

public sealed class ProductEndpointsTests
{
    private static readonly string[] SampleImages = ["https://example.test/a.png"];

    [Fact]
    public async Task GetProducts_ReturnsTheCollection()
    {
        using var factory = ApiFactory.RespondingWith(HttpStatusCode.OK, """[{"id":1,"title":"One"}]""");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/products", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_SucceedsRepeatedly()
    {
        // The regression test for audit finding #1. The original service was a
        // singleton that assigned HttpClient.BaseAddress on every call, so the
        // second request onward threw InvalidOperationException and returned 500.
        // Three calls through the real DI graph is what would catch a reintroduction.
        using var factory = ApiFactory.RespondingWith(HttpStatusCode.OK, "[]");
        using var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var response = await client.GetAsync(new Uri("/api/v1/products", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task GetProductById_Returns404_WhenUpstreamReportsEntityNotFound()
    {
        // End to end: upstream 400 + EntityNotFoundError becomes a 404 with a
        // ProblemDetails body. Exercises the service translation, the exception
        // handler mapping and the ProblemDetails writer together.
        using var factory = ApiFactory.RespondingWith(HttpStatusCode.BadRequest,
            """{"name":"EntityNotFoundError","message":"Could not find any entity of type \"Product\""}""");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/products/999", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(StatusCodes.Status404NotFound, problem!.Status);
    }

    [Fact]
    public async Task CreateProduct_Returns400WithFieldErrors_ForAnEmptyBody()
    {
        // Upstream answers this with an opaque 500. The whole point of validating
        // first is that the caller gets something actionable instead.
        using var factory = ApiFactory.RespondingWith(HttpStatusCode.Created, "{}");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(new Uri("/api/v1/products", UriKind.Relative), new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Contains(nameof(ProductWriteModel.Title), problem!.Errors.Keys);
        Assert.Contains(nameof(ProductWriteModel.Price), problem.Errors.Keys);
        Assert.Contains(nameof(ProductWriteModel.CategoryId), problem.Errors.Keys);

        // The request must never have reached upstream: only the token exchange
        // should have happened.
        Assert.DoesNotContain(factory.Upstream.Requests,
            request => request.Method == HttpMethod.Post &&
                       request.Uri!.AbsolutePath.EndsWith("/products", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CreateProduct_Returns201WithLocation()
    {
        using var factory = ApiFactory.RespondingWith(HttpStatusCode.Created, """{"id":42,"title":"Created"}""");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(new Uri("/api/v1/products", UriKind.Relative), new
        {
            title = "Created",
            price = 9.99m,
            description = "d",
            categoryId = 1,
            images = SampleImages,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/v1/products/42", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task DeleteProduct_Returns204()
    {
        // Upstream answers a delete with 200 and a bare "true"; this API answers 204.
        using var factory = ApiFactory.RespondingWith(HttpStatusCode.OK, "true");
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync(new Uri("/api/v1/products/1", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Endpoints_AttachABearerToken()
    {
        // Proves the bearer handler is wired into the product client in the real
        // container, not merely that the handler works in isolation.
        using var factory = ApiFactory.RespondingWith(HttpStatusCode.OK, "[]");
        using var client = factory.CreateClient();

        await client.GetAsync(new Uri("/api/v1/products", UriKind.Relative));

        var productCall = Assert.Single(factory.Upstream.Requests,
            request => request.Uri!.AbsolutePath.EndsWith("/products", StringComparison.Ordinal));

        Assert.StartsWith("Bearer ", productCall.Authorization, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthClient_DoesNotCarryABearerToken()
    {
        // The recursion guard. If the auth client ever gained the bearer handler,
        // acquiring a token would require a token.
        using var factory = ApiFactory.RespondingWith(HttpStatusCode.OK, "[]");
        using var client = factory.CreateClient();

        await client.GetAsync(new Uri("/api/v1/products", UriKind.Relative));

        var authCall = Assert.Single(factory.Upstream.Requests,
            request => request.Uri!.AbsolutePath.Contains("/auth/", StringComparison.Ordinal));

        Assert.Null(authCall.Authorization);
    }
}
