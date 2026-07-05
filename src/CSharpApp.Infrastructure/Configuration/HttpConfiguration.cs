using Microsoft.Extensions.Options;

namespace CSharpApp.Infrastructure.Configuration;

public static class HttpConfiguration
{
    public static IServiceCollection AddHttpConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Register a typed HttpClient for ProductsService using IHttpClientFactory
        services.AddHttpClient<IProductsService, ProductsService>((sp, client) =>
        {
            var rest = sp.GetRequiredService<IOptions<RestApiSettings>>().Value;
            var httpSettings = sp.GetRequiredService<IOptions<HttpClientSettings>>().Value;

            if (!string.IsNullOrWhiteSpace(rest.BaseUrl))
            {
                client.BaseAddress = new Uri(rest.BaseUrl);
            }

            // Configure timeout from settings (LifeTime is seconds)
            if (httpSettings.LifeTime > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(httpSettings.LifeTime);
            }
        });

        return services;
    }
}
