using System.Net;
using ApplicationTests.TestSupport;
using CSharpApp.Application.Auth;
using CSharpApp.Core.Settings;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace ApplicationTests.Auth;

public class AuthApiClientTests
{
	private static RestApiSettings Settings() => new()
	{
		BaseUrl = "https://example.com/",
		Auth = "auth/login",
		RefreshToken = "auth/refresh-token",
		Username = "john@mail.com",
		Password = "changeme"
	};

	private static AuthApiClient CreateSut(StubHttpMessageHandler stub)
		=> new(new HttpClient(stub), Options.Create(Settings()));

	[Fact]
	public async Task Login_sends_the_configured_credentials_and_parses_the_tokens()
	{
		var stub = new StubHttpMessageHandler(
			StubHttpMessageHandler.Json(HttpStatusCode.Created, "{\"access_token\":\"a\",\"refresh_token\":\"r\"}"));

		var tokens = await CreateSut(stub).Login(CancellationToken.None);

		tokens.AccessToken.Should().Be("a");
		tokens.RefreshToken.Should().Be("r");
		stub.Bodies[0].Should().Contain("john@mail.com").And.Contain("changeme");
		stub.Bodies[0].Should().Contain("email");
	}

	[Fact]
	public async Task Login_throws_when_the_downstream_call_fails()
	{
		var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
		var sut = CreateSut(stub);

		Func<Task> act = () => sut.Login(CancellationToken.None);

		await act.Should().ThrowAsync<HttpRequestException>();
	}

	[Fact]
	public async Task Refresh_returns_tokens_on_success()
	{
		var stub = new StubHttpMessageHandler(
			StubHttpMessageHandler.Json(HttpStatusCode.Created, "{\"access_token\":\"a2\",\"refresh_token\":\"r2\"}"));

		var tokens = await CreateSut(stub).Refresh("old-refresh", CancellationToken.None);

		tokens.Should().NotBeNull();
		tokens!.AccessToken.Should().Be("a2");
	}

	[Fact]
	public async Task Refresh_returns_null_when_the_refresh_call_is_rejected()
	{
		var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));

		var tokens = await CreateSut(stub).Refresh("dead-refresh", CancellationToken.None);

		tokens.Should().BeNull();
	}
}