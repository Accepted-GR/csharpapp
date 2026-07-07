using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using CSharpApp.Application.Categories.Queries;
using CSharpApp.Application.Categories.Commands;

namespace CSharpApp.Tests;

public class CategoryQueryHandlerTests
{
    [Fact]
    public async Task GetCategoryById_ReturnsCategory_WhenFound()
    {
        // Arrange
        var categoryJson = "{ \"id\": 2, \"name\": \"Electronics\", \"image\": \"/img.png\" }";
        var handler = new DelegatingHandlerStub((request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(categoryJson)
            };
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.test/")
        };

        var options = Options.Create(new CSharpApp.Core.Settings.RestApiSettings { Categories = "categories" });
        var logger = NullLogger<GetCategoryByIdQueryHandler>.Instance;

        var queryHandler = new GetCategoryByIdQueryHandler(httpClient, options, logger);
        var query = new GetCategoryByIdQuery(2);

        // Act
        var result = await queryHandler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result!.Id);
        Assert.Equal("Electronics", result.Name);
    }
}

public class CategoryCommandHandlerTests
{
    [Fact]
    public async Task CreateCategory_ReturnsCreatedCategory_WhenSuccess()
    {
        // Arrange
        var category = new CSharpApp.Core.Dtos.Category { Name = "Books", Image = "/books.png" };
        var responseJson = "{ \"id\": 20, \"name\": \"Books\", \"image\": \"/books.png\" }";

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
            BaseAddress = new Uri("https://api.test/")
        };

        var options = Options.Create(new CSharpApp.Core.Settings.RestApiSettings { Categories = "categories" });
        var logger = NullLogger<CreateCategoryCommandHandler>.Instance;

        var commandHandler = new CreateCategoryCommandHandler(httpClient, options, logger);
        var command = new CreateCategoryCommand(category);

        // Act
        var created = await commandHandler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(20, created!.Id);
        Assert.Equal("Books", created.Name);
    }
}
