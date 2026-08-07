namespace CSharpApp.Tests.Products;

public sealed class ProductsServiceTests
{
    private const string EntityNotFoundBody =
        """{"path":"/api/v1/products/999","name":"EntityNotFoundError","message":"Could not find any entity of type \"Product\""}""";

    [Fact]
    public async Task GetProductsAsync_ReturnsTheProducts()
    {
        using var fixture = Fixture.Returning(HttpStatusCode.OK,
            """[{"id":1,"title":"One"},{"id":2,"title":"Two"}]""");

        var products = await fixture.Service.GetProductsAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetProductsAsync_PopulatesImages()
    {
        // Regression test for audit finding #4. Images was declared get-only, which
        // System.Text.Json skips, so every upstream image URL was silently dropped
        // and the field serialized back out as an empty array with no error. This
        // fails if the setter is ever removed.
        using var fixture = Fixture.Returning(HttpStatusCode.OK,
            """[{"id":1,"images":["https://example.test/a.png","https://example.test/b.png"]}]""");

        var product = Assert.Single(await fixture.Service.GetProductsAsync());

        Assert.Equal(2, product.Images.Count);
        Assert.Equal("https://example.test/a.png", product.Images[0]);
    }

    [Fact]
    public async Task GetProductsAsync_ReturnsEmpty_ForANullPayload()
    {
        // A literal "null" body is well-formed JSON but not a usable result. This is
        // the CS8602 from the original code, asserted rather than suppressed.
        using var fixture = Fixture.Returning(HttpStatusCode.OK, "null");

        Assert.Empty(await fixture.Service.GetProductsAsync());
    }

    [Fact]
    public async Task GetProductByIdAsync_Throws_WhenUpstreamReportsEntityNotFound()
    {
        // The upstream API answers a missing entity with 400 and an
        // EntityNotFoundError body rather than 404. Translating it is what lets the
        // API report an honest 404 instead of telling callers their request was
        // malformed.
        using var fixture = Fixture.Returning(HttpStatusCode.BadRequest, EntityNotFoundBody);

        await Assert.ThrowsAsync<UpstreamNotFoundException>(
            () => fixture.Service.GetProductByIdAsync(999));
    }

    [Fact]
    public async Task GetProductByIdAsync_DoesNotTreatAPlainBadRequest_AsNotFound()
    {
        // The distinction the body inspection exists for. A genuinely malformed
        // request must stay a bad request, not become a 404.
        using var fixture = Fixture.Returning(HttpStatusCode.BadRequest,
            """{"message":"price must be a positive number"}""");

        await Assert.ThrowsAsync<HttpRequestException>(
            () => fixture.Service.GetProductByIdAsync(1));
    }

    [Fact]
    public async Task GetProductByIdAsync_Throws_WhenUpstreamReturns404()
    {
        // Upstream does not currently use 404, but handling it costs nothing and
        // means a future upstream fix does not break this client.
        using var fixture = Fixture.Returning(HttpStatusCode.NotFound, "{}");

        await Assert.ThrowsAsync<UpstreamNotFoundException>(
            () => fixture.Service.GetProductByIdAsync(999));
    }

    [Fact]
    public async Task CreateProductAsync_SendsTheWriteContract()
    {
        using var fixture = Fixture.Returning(HttpStatusCode.Created, """{"id":7,"title":"Created"}""");

        await fixture.Service.CreateProductAsync(new ProductWriteModel
        {
            Title = "Created",
            Price = 12.5m,
            Description = "d",
            CategoryId = 3,
            Images = ["https://example.test/a.png"],
        });

        var body = fixture.Handler.Requests[0].Body!;

        // categoryId flat, not a nested category object: the write contract is not
        // the read contract.
        Assert.Contains("\"categoryId\":3", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"category\":", body, StringComparison.Ordinal);

        // Server-owned fields must never be sent.
        Assert.DoesNotContain("\"id\":", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"slug\":", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateProductAsync_Throws_WhenTheCategoryDoesNotExist()
    {
        // A create can 404 too: upstream rejects an unknown categoryId with the same
        // EntityNotFoundError shape it uses for a missing product.
        using var fixture = Fixture.Returning(HttpStatusCode.BadRequest,
            """{"name":"EntityNotFoundError","message":"Could not find any entity of type \"Category\""}""");

        await Assert.ThrowsAsync<UpstreamNotFoundException>(
            () => fixture.Service.CreateProductAsync(new ProductWriteModel
            {
                Title = "t",
                Price = 1m,
                Description = "d",
                CategoryId = 999_999,
                Images = ["https://example.test/a.png"],
            }));
    }

    [Fact]
    public async Task UpdateProductAsync_UsesPut()
    {
        using var fixture = Fixture.Returning(HttpStatusCode.OK, """{"id":5}""");

        await fixture.Service.UpdateProductAsync(5, new ProductWriteModel
        {
            Title = "t",
            Price = 1m,
            Description = "d",
            CategoryId = 1,
            Images = ["https://example.test/a.png"],
        });

        Assert.Equal(HttpMethod.Put, fixture.Handler.Requests[0].Method);
        Assert.EndsWith("/products/5", fixture.Handler.Requests[0].Uri!.AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteProductAsync_Throws_WhenTheProductIsAlreadyGone()
    {
        using var fixture = Fixture.Returning(HttpStatusCode.BadRequest, EntityNotFoundBody);

        await Assert.ThrowsAsync<UpstreamNotFoundException>(
            () => fixture.Service.DeleteProductAsync(999));
    }

    [Fact]
    public async Task DeleteProductAsync_Succeeds_OnTheUpstreamTrueResponse()
    {
        // Upstream answers a delete with 200 and a bare "true".
        using var fixture = Fixture.Returning(HttpStatusCode.OK, "true");

        await fixture.Service.DeleteProductAsync(5);

        Assert.Equal(HttpMethod.Delete, fixture.Handler.Requests[0].Method);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly HttpClient _client;

        private Fixture(StubHttpMessageHandler handler)
        {
            Handler = handler;

            _client = new HttpClient(handler) { BaseAddress = new Uri("https://upstream.test/api/v1/") };

            Service = new ProductsService(
                _client,
                Options.Create(new RestApiSettings { Products = "products" }),
                NullLogger<ProductsService>.Instance);
        }

        public StubHttpMessageHandler Handler { get; }

        public ProductsService Service { get; }

        public static Fixture Returning(HttpStatusCode status, string json) =>
            new(StubHttpMessageHandler.Always(status, json));

        public void Dispose() => _client.Dispose();
    }
}
