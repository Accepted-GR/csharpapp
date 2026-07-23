using CSharpApp.Application.Auth;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Interfaces;
using CSharpApp.Core.Settings;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ApplicationTests.Auth;

public class AuthServiceTests
{
	private readonly Mock<IAuthApiClient> _apiClient = new();

	private AuthService CreateSut()
	{
		var options = Options.Create(new CachingConfiguration { AuthTokenCacheLifetime = TimeSpan.FromHours(1) });
		return new AuthService(_apiClient.Object, CreateRealHybridCache(), options, NullLogger<AuthService>.Instance);
	}

	// HybridCache is impractical to mock; the recommended approach is to test against the real in-memory implementation.
	private static HybridCache CreateRealHybridCache()
	{
		var services = new ServiceCollection();
		services.AddHybridCache();
		return services.BuildServiceProvider().GetRequiredService<HybridCache>();
	}

	[Fact]
	public async Task GetAccessToken_logs_in_once_and_caches_the_token()
	{
		_apiClient.Setup(c => c.Login(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new AuthTokens { AccessToken = "access-1", RefreshToken = "refresh-1" });

		var sut = CreateSut();

		var first = await sut.GetAccessToken(CancellationToken.None);
		var second = await sut.GetAccessToken(CancellationToken.None);

		first.Should().Be("access-1");
		second.Should().Be("access-1");
		_apiClient.Verify(c => c.Login(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task RefreshAccessToken_uses_the_refresh_token_when_available()
	{
		_apiClient.Setup(c => c.Login(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new AuthTokens { AccessToken = "access-1", RefreshToken = "refresh-1" });
		_apiClient.Setup(c => c.Refresh("refresh-1", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new AuthTokens { AccessToken = "access-2", RefreshToken = "refresh-2" });

		var sut = CreateSut();
		await sut.GetAccessToken(CancellationToken.None); // warm the cache

		var refreshed = await sut.RefreshAccessToken(CancellationToken.None);

		refreshed.Should().Be("access-2");
		_apiClient.Verify(c => c.Refresh("refresh-1", It.IsAny<CancellationToken>()), Times.Once);
		_apiClient.Verify(c => c.Login(It.IsAny<CancellationToken>()), Times.Once); // no extra login
	}

	[Fact]
	public async Task RefreshAccessToken_falls_back_to_login_when_refresh_fails()
	{
		_apiClient.SetupSequence(c => c.Login(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new AuthTokens { AccessToken = "access-1", RefreshToken = "refresh-1" })
			.ReturnsAsync(new AuthTokens { AccessToken = "access-relogin", RefreshToken = "refresh-new" });
		_apiClient.Setup(c => c.Refresh("refresh-1", It.IsAny<CancellationToken>()))
			.ReturnsAsync((AuthTokens?)null);

		var sut = CreateSut();
		await sut.GetAccessToken(CancellationToken.None);

		var result = await sut.RefreshAccessToken(CancellationToken.None);

		result.Should().Be("access-relogin");
		_apiClient.Verify(c => c.Refresh("refresh-1", It.IsAny<CancellationToken>()), Times.Once);
		_apiClient.Verify(c => c.Login(It.IsAny<CancellationToken>()), Times.Exactly(2));
	}

	[Fact]
	public async Task RefreshAccessToken_logs_in_when_no_refresh_token_is_present()
	{
		_apiClient.SetupSequence(c => c.Login(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new AuthTokens { AccessToken = "access-1", RefreshToken = "" })
			.ReturnsAsync(new AuthTokens { AccessToken = "access-relogin", RefreshToken = "" });

		var sut = CreateSut();
		await sut.GetAccessToken(CancellationToken.None);

		var result = await sut.RefreshAccessToken(CancellationToken.None);

		result.Should().Be("access-relogin");
		_apiClient.Verify(c => c.Refresh(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
		_apiClient.Verify(c => c.Login(It.IsAny<CancellationToken>()), Times.Exactly(2));
	}
}
