using CSharpApp.Core.Constants;
using Microsoft.Extensions.Caching.Hybrid;

namespace CSharpApp.Application.Auth
{
	public class AuthService : IAuthService
	{
		private readonly IAuthApiClient _authApiClient;
		private readonly HybridCache _cache;
		private readonly HybridCacheEntryOptions _cacheOptions;
		private readonly ILogger<AuthService> _logger;

		public AuthService(IAuthApiClient authApiClient, HybridCache cache,
			IOptions<CachingConfiguration> cachingConfiguration, ILogger<AuthService> logger)
		{
			_authApiClient = authApiClient;
			_cache = cache;
			_logger = logger;
			_cacheOptions = new HybridCacheEntryOptions
			{
				Expiration = cachingConfiguration.Value.AuthTokenCacheLifetime
			};
		}

		public async Task<string> GetAccessToken(CancellationToken cancellationToken)
		{
			var tokens = await GetTokens(cancellationToken);
			return tokens.AccessToken;
		}

		public async Task<string> RefreshAccessToken(CancellationToken cancellationToken)
		{
			var current = await GetTokens(cancellationToken);

			var refreshed = !string.IsNullOrEmpty(current.RefreshToken)
				? await _authApiClient.Refresh(current.RefreshToken, cancellationToken)
				: null;

			if (refreshed is not null)
				_logger.LogInformation("Access token refreshed.");
			else
				refreshed = await Login(cancellationToken);

			await _cache.SetAsync(CacheName.AuthToken, refreshed, _cacheOptions, cancellationToken: cancellationToken);
			return refreshed.AccessToken;
		}

		private async ValueTask<AuthTokens> GetTokens(CancellationToken cancellationToken)
			=> await _cache.GetOrCreateAsync(CacheName.AuthToken,
				async ct => await Login(ct),
				_cacheOptions,
				cancellationToken: cancellationToken);

		private async Task<AuthTokens> Login(CancellationToken cancellationToken)
		{
			var tokens = await _authApiClient.Login(cancellationToken);
			_logger.LogInformation("Logged in to the downstream API.");
			return tokens;
		}
	}
}