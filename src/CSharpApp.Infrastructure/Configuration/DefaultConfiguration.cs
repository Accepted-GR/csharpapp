using CSharpApp.Application.Products.Queries;

namespace CSharpApp.Infrastructure.Configuration;

public static class DefaultConfiguration
{
	public static IServiceCollection AddDefaultConfiguration(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<RestApiSettings>(configuration.GetSection(nameof(RestApiSettings)));
		services.Configure<HttpClientSettings>(configuration.GetSection(nameof(HttpClientSettings)));

		//Add Clients
		services.AddHttpClient<IProductsService, ProductsService>();
		services.AddHttpClient<ICategoriesService, CategoriesService>();

		//Add Mediator
		services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetProductsQuery).Assembly));

		return services;
	}
}