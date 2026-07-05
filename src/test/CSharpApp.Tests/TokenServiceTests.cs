using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CSharpApp.Tests;

public class TokenServiceTests
{
    [Fact]
    public async Task AcquireToken_ReturnsTokenAndCaches_WhenAuthSuccess()
    {
        // Arrange
        var tokenJson = "{ \"access_token\": \"abc123\", \"expires_in\": 3600 }";
        var callCount = 0;
        var handler = new DelegatingHandlerStub((request, ct) =>
        {
            callCount++;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(tokenJson)
            };
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth.test/") };
        var options = Options.Create(new CSharpApp.Core.Settings.RestApiSettings { Auth = "/auth/login", Username = "u", Password = "p" });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<CSharpApp.Infrastructure.Authentication.TokenService>.Instance;

        var svc = new CSharpApp.Infrastructure.Authentication.TokenService(httpClient, options, cache, logger);

        // Act
        var token1 = await svc.GetTokenAsync(CancellationToken.None);
        var token2 = await svc.GetTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal("abc123", token1);
        Assert.Equal("abc123", token2);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task AcquireToken_Throws_OnNonSuccessStatusCode()
    {
        // Arrange
        var handler = new DelegatingHandlerStub((request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth.test/") };
        var options = Options.Create(new CSharpApp.Core.Settings.RestApiSettings { Auth = "/auth/login", Username = "u", Password = "p" });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = NullLogger<CSharpApp.Infrastructure.Authentication.TokenService>.Instance;

        var svc = new CSharpApp.Infrastructure.Authentication.TokenService(httpClient, options, cache, logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.GetTokenAsync(CancellationToken.None));
    }
}
