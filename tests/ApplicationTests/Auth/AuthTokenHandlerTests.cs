using System.Net;
using ApplicationTests.TestSupport;
using CSharpApp.Application.Auth;
using CSharpApp.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace ApplicationTests.Auth;

public class AuthTokenHandlerTests
{
	private readonly Mock<IAuthService> _authService = new();

	private HttpMessageInvoker CreateInvoker(StubHttpMessageHandler inner)
	{
		var handler = new AuthTokenHandler(_authService.Object) { InnerHandler = inner };
		return new HttpMessageInvoker(handler);
	}

	[Fact]
	public async Task Attaches_the_bearer_token_and_does_not_refresh_on_success()
	{
		_authService.Setup(s => s.GetAccessToken(It.IsAny<CancellationToken>())).ReturnsAsync("token-1");
		var inner = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));

		var response = await CreateInvoker(inner).SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/products"), CancellationToken.None);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		inner.CallCount.Should().Be(1);
		inner.AuthorizationParameters.Should().ContainSingle().Which.Should().Be("token-1");
		_authService.Verify(s => s.RefreshAccessToken(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task Refreshes_and_retries_once_on_401()
	{
		_authService.Setup(s => s.GetAccessToken(It.IsAny<CancellationToken>())).ReturnsAsync("token-1");
		_authService.Setup(s => s.RefreshAccessToken(It.IsAny<CancellationToken>())).ReturnsAsync("token-2");
		var inner = new StubHttpMessageHandler(
			new HttpResponseMessage(HttpStatusCode.Unauthorized),
			new HttpResponseMessage(HttpStatusCode.OK));

		var response = await CreateInvoker(inner).SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/products"), CancellationToken.None);

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		inner.CallCount.Should().Be(2);
		inner.AuthorizationParameters.Should().Equal("token-1", "token-2"); // first attempt then refreshed retry
		_authService.Verify(s => s.RefreshAccessToken(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Retries_only_once_then_returns_the_second_401()
	{
		_authService.Setup(s => s.GetAccessToken(It.IsAny<CancellationToken>())).ReturnsAsync("token-1");
		_authService.Setup(s => s.RefreshAccessToken(It.IsAny<CancellationToken>())).ReturnsAsync("token-2");
		var inner = new StubHttpMessageHandler(
			new HttpResponseMessage(HttpStatusCode.Unauthorized),
			new HttpResponseMessage(HttpStatusCode.Unauthorized));

		var response = await CreateInvoker(inner).SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://x/products"), CancellationToken.None);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
		inner.CallCount.Should().Be(2); // no infinite loop
		_authService.Verify(s => s.RefreshAccessToken(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task Replays_the_request_body_on_the_retry()
	{
		_authService.Setup(s => s.GetAccessToken(It.IsAny<CancellationToken>())).ReturnsAsync("token-1");
		_authService.Setup(s => s.RefreshAccessToken(It.IsAny<CancellationToken>())).ReturnsAsync("token-2");
		var inner = new StubHttpMessageHandler(
			new HttpResponseMessage(HttpStatusCode.Unauthorized),
			new HttpResponseMessage(HttpStatusCode.Created));

		var request = new HttpRequestMessage(HttpMethod.Post, "https://x/products")
		{
			Content = new StringContent("{\"title\":\"hat\"}", System.Text.Encoding.UTF8, "application/json")
		};

		var response = await CreateInvoker(inner).SendAsync(request, CancellationToken.None);

		response.StatusCode.Should().Be(HttpStatusCode.Created);
		inner.Bodies.Should().Equal("{\"title\":\"hat\"}", "{\"title\":\"hat\"}"); // body survived the clone
	}
}
