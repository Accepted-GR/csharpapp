namespace CSharpApp.Tests.Integration;

/// <summary>
/// The response-header half of the performance middleware.
/// </summary>
/// <remarks>
/// Lives at the integration level because the headers are written from an
/// OnStarting callback. DefaultHttpContext's stub response feature discards those
/// callbacks, so a unit test could only ever assert the header was absent; a real
/// server fires them.
/// </remarks>
public sealed class PerformanceHeadersTests
{
    [Fact]
    public async Task Response_CarriesAResponseTimeHeader()
    {
        using var factory = ApiFactory.RespondingWith(HttpStatusCode.OK, "[]");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/products", UriKind.Relative));

        Assert.True(response.Headers.TryGetValues("X-Response-Time-Ms", out var values));

        Assert.True(
            double.TryParse(values!.Single(), CultureInfo.InvariantCulture, out var elapsed),
            "X-Response-Time-Ms should be a number.");

        Assert.True(elapsed >= 0);
    }

    [Fact]
    public async Task Response_EchoesASuppliedCorrelationId()
    {
        // Honouring an inbound id is what allows one request to be traced across
        // service boundaries rather than only within this one.
        using var factory = ApiFactory.RespondingWith(HttpStatusCode.OK, "[]");
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/v1/products", UriKind.Relative));
        request.Headers.Add("X-Correlation-Id", "trace-from-caller");

        var response = await client.SendAsync(request);

        Assert.Equal("trace-from-caller", response.Headers.GetValues("X-Correlation-Id").Single());
    }

    [Fact]
    public async Task Response_GeneratesACorrelationId_WhenNoneIsSupplied()
    {
        using var factory = ApiFactory.RespondingWith(HttpStatusCode.OK, "[]");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/products", UriKind.Relative));

        Assert.NotEmpty(response.Headers.GetValues("X-Correlation-Id").Single());
    }

    [Fact]
    public async Task Response_CarriesHeaders_EvenOnAnErrorResponse()
    {
        // The measurement lives in a finally block precisely so failures are still
        // timed; the headers must survive the same path.
        using var factory = ApiFactory.RespondingWith(HttpStatusCode.BadRequest,
            """{"name":"EntityNotFoundError","message":"missing"}""");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/products/999", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Response-Time-Ms"));
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }
}
