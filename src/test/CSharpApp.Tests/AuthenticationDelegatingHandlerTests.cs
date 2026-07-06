using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using Xunit;

namespace CSharpApp.Tests;

public class AuthenticationDelegatingHandlerTests
{
    [Fact]
    public async Task AddsAuthorizationHeader_WhenTokenAvailable()
    {
        // Arrange
        var fakeTokenService = new FakeTokenService(() => Task.FromResult("token1"));

        var innerHandler = new DelegatingHandlerStub((request, ct) =>
        {
            Assert.True(request.Headers.Authorization != null);
            Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
            Assert.Equal("token1", request.Headers.Authorization.Parameter);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var authHandler = new CSharpApp.Infrastructure.Authentication.AuthenticationDelegatingHandler(fakeTokenService, NullLogger<CSharpApp.Infrastructure.Authentication.AuthenticationDelegatingHandler>.Instance)
        {
            InnerHandler = innerHandler
        };

        var invoker = new HttpMessageInvoker(authHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test/");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RetriesOn401_RefreshesToken()
    {
        // Arrange
        var tokens = new Queue<string>(new[] { "token1", "token2" });
        var fakeTokenService = new FakeTokenService(() => Task.FromResult(tokens.Dequeue()));

        var call = 0;
        var innerHandler = new DelegatingHandlerStub((request, ct) =>
        {
            call++;
            if (call == 1)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            Assert.Equal("token2", request.Headers.Authorization.Parameter);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var authHandler = new CSharpApp.Infrastructure.Authentication.AuthenticationDelegatingHandler(fakeTokenService, NullLogger<CSharpApp.Infrastructure.Authentication.AuthenticationDelegatingHandler>.Instance)
        {
            InnerHandler = innerHandler
        };

        var invoker = new HttpMessageInvoker(authHandler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test/");

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, call);
    }

    [Fact]
    public async Task PropagatesException_WhenTokenServiceFails()
    {
        // Arrange
        var fakeTokenService = new FakeTokenService(() => throw new InvalidOperationException("fail"));

        var innerHandler = new DelegatingHandlerStub((request, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var authHandler = new CSharpApp.Infrastructure.Authentication.AuthenticationDelegatingHandler(fakeTokenService, NullLogger<CSharpApp.Infrastructure.Authentication.AuthenticationDelegatingHandler>.Instance)
        {
            InnerHandler = innerHandler
        };

        var invoker = new HttpMessageInvoker(authHandler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test/");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => invoker.SendAsync(request, CancellationToken.None));
    }
}

internal class FakeTokenService : CSharpApp.Core.Interfaces.ITokenService
{
    private readonly Func<Task<string>> _responder;

    public FakeTokenService(Func<Task<string>> responder)
    {
        _responder = responder;
    }

    public Task<string> GetTokenAsync(CancellationToken cancellationToken = default) => _responder();
}
