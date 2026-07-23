using CSharpApp.Application.Auth;
using CSharpApp.Application.Products.Queries;

namespace CSharpApp.Infrastructure.Configuration;

public static class DefaultConfiguration
{
	public static IServiceCollection AddDefaultConfiguration(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<RestApiSettings>(configuration.GetSection(nameof(RestApiSettings)));
		services.Configure<HttpClientSettings>(configuration.GetSection(nameof(HttpClientSettings)));
		services.Configure<CachingConfiguration>(configuration.GetSection(nameof(CachingConfiguration)));

		//Add Caching
		services.AddHybridCache();

		//Add Auth
		services.AddTransient<IAuthService, AuthService>();
		services.AddTransient<AuthTokenHandler>();

		//Add Mediator
		services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetProductsQuery).Assembly));

		return services;
	}
}
