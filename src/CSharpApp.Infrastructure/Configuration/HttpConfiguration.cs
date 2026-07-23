using CSharpApp.Application.Auth;
using Polly;
using Polly.Extensions.Http;

namespace CSharpApp.Infrastructure.Configuration;

public static class HttpConfiguration
{
	public static IServiceCollection AddHttpConfiguration(this IServiceCollection services, IConfiguration configuration)
	{
		var httpClientSettings = configuration.GetSection(nameof(HttpClientSettings)).Get<HttpClientSettings>()
			?? new HttpClientSettings();

		var handlerLifetime = TimeSpan.FromMinutes(httpClientSettings.LifeTime);

		// Auth API client logs in / refreshes tokens. It deliberately does NOT carry AuthTokenHandler,
		// otherwise acquiring a token would itself require a token (infinite loop).
		services.AddHttpClient<IAuthApiClient, AuthApiClient>()
			.SetHandlerLifetime(handlerLifetime)
			.AddPolicyHandler(GetRetryPolicy(httpClientSettings));

		// Downstream clients: Polly (outer) for transient transport faults, AuthTokenHandler (inner) for the bearer token.
		services.AddHttpClient<IProductsService, ProductsService>()
			.SetHandlerLifetime(handlerLifetime)
			.AddPolicyHandler(GetRetryPolicy(httpClientSettings))
			.AddHttpMessageHandler<AuthTokenHandler>();

		services.AddHttpClient<ICategoriesService, CategoriesService>()
			.SetHandlerLifetime(handlerLifetime)
			.AddPolicyHandler(GetRetryPolicy(httpClientSettings))
			.AddHttpMessageHandler<AuthTokenHandler>();

		return services;
	}

	private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(HttpClientSettings settings)
		=> HttpPolicyExtensions
			.HandleTransientHttpError()
			.WaitAndRetryAsync(
				settings.RetryCount,
				_ => TimeSpan.FromMilliseconds(settings.SleepDuration));
}