using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using CSharpApp.Application.Products.Queries;
using CSharpApp.Application.Products.Commands;

namespace CSharpApp.Tests;

public class ProductQueryHandlerTests
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
        var logger = NullLogger<GetProductByIdQueryHandler>.Instance;

        var queryHandler = new GetProductByIdQueryHandler(httpClient, options, logger);
        var query = new GetProductByIdQuery(1);

        // Act
        var result = await queryHandler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        Assert.Equal("Test", result.Title);
    }
}

public class ProductCommandHandlerTests
{
    [Fact]
    public async Task CreateProduct_ReturnsCreatedProduct_WhenSuccess()
    {
        // Arrange
        var product = new CSharpApp.Core.Dtos.Product { Title = "New", Price = 50m };
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
        var logger = NullLogger<CreateProductCommandHandler>.Instance;

        var commandHandler = new CreateProductCommandHandler(httpClient, options, logger);
        var command = new CreateProductCommand(product);

        // Act
        var created = await commandHandler.Handle(command, CancellationToken.None);

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
