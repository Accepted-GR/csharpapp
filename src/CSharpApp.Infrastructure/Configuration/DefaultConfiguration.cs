namespace CSharpApp.Infrastructure.Configuration;

public static class DefaultConfiguration
{
    public static IServiceCollection AddDefaultConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration sections to options without building a temporary provider
        services.Configure<RestApiSettings>(configuration.GetSection(nameof(RestApiSettings)));
        services.Configure<HttpClientSettings>(configuration.GetSection(nameof(HttpClientSettings)));

        // Register product validation and mapping
        services.AddSingleton<CSharpApp.Application.Products.IProductValidator, CSharpApp.Application.Products.ProductValidator>();
        services.AddSingleton<CSharpApp.Application.Products.IProductMapper, CSharpApp.Application.Products.ProductMapper>();
        // Register infrastructure adapter for upstream mapping
        services.AddSingleton<CSharpApp.Application.Products.IUpstreamProductMapper, CSharpApp.Infrastructure.Adapters.UpstreamProductMapper>();

        return services;
    }
}
