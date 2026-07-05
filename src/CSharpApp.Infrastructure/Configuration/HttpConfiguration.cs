using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using CSharpApp.Application.Categories;
using CSharpApp.Application.Products;

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

        // Register typed client for categories
        services.AddHttpClient<ICategoriesService, CategoriesService>((sp, client) =>
        {
            var rest = sp.GetRequiredService<IOptions<RestApiSettings>>().Value;
            var httpSettings = sp.GetRequiredService<IOptions<HttpClientSettings>>().Value;

            if (!string.IsNullOrWhiteSpace(rest.BaseUrl))
            {
                client.BaseAddress = new Uri(rest.BaseUrl);
            }

            if (httpSettings.LifeTime > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(httpSettings.LifeTime);
            }
        });

        return services;
    }
}
