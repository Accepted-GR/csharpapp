using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CSharpApp.Tests;

public class ProductsServiceTests
{
    [Fact]
    public async Task GetProductById_ReturnsProduct_WhenFound()
    {
        // Arrange
        var productJson = "{ \"id\": 1, \"title\": \"Test\", \"price\": 100 }";
        var handler = new DelegatingHandlerStub((request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(productJson)
            };
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new System.Uri("https://api.test/")
        };

        var options = Options.Create(new CSharpApp.Core.Settings.RestApiSettings { Products = "products" });
        var logger = NullLogger<CSharpApp.Application.Products.ProductsService>.Instance;

        var svc = new CSharpApp.Application.Products.ProductsService(httpClient, options, logger);

        // Act
        var result = await svc.GetProductById(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        Assert.Equal("Test", result.Title);
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreatedProduct_WhenSuccess()
    {
        // Arrange
        var product = new CSharpApp.Core.Dtos.Product { Title = "New", Price = 50 };
        var responseJson = "{ \"id\": 10, \"title\": \"New\", \"price\": 50 }";

        var handler = new DelegatingHandlerStub((request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(responseJson)
            };
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new System.Uri("https://api.test/")
        };

        var options = Options.Create(new CSharpApp.Core.Settings.RestApiSettings { Products = "products" });
        var logger = NullLogger<CSharpApp.Application.Products.ProductsService>.Instance;

        var svc = new CSharpApp.Application.Products.ProductsService(httpClient, options, logger);

        // Act
        var created = await svc.CreateProduct(product, CancellationToken.None);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(10, created!.Id);
        Assert.Equal("New", created.Title);
    }
}

// Helper stub handler
internal class DelegatingHandlerStub : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

    public DelegatingHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _responder.Invoke(request, cancellationToken);
    }
}
