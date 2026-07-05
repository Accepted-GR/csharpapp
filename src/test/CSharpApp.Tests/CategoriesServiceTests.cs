using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CSharpApp.Tests;

public class CategoriesServiceTests
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
        var logger = NullLogger<CSharpApp.Application.Categories.CategoriesService>.Instance;

        var svc = new CSharpApp.Application.Categories.CategoriesService(httpClient, options, logger);

        // Act
        var result = await svc.GetCategoryById(2, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result!.Id);
        Assert.Equal("Electronics", result.Name);
    }

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
        var logger = NullLogger<CSharpApp.Application.Categories.CategoriesService>.Instance;

        var svc = new CSharpApp.Application.Categories.CategoriesService(httpClient, options, logger);

        // Act
        var created = await svc.CreateCategory(category, CancellationToken.None);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(20, created!.Id);
        Assert.Equal("Books", created.Name);
    }
}
