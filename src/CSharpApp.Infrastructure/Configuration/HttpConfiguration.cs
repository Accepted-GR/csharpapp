using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using CSharpApp.Application.Categories;
using CSharpApp.Application.Products;
using CSharpApp.Infrastructure.Authentication;
using CSharpApp.Infrastructure.Http;
using CSharpApp.Core.Interfaces;

namespace CSharpApp.Infrastructure.Configuration;

public static class HttpConfiguration
{
    public static IServiceCollection AddHttpConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Register token service, memory cache and authentication handler
        services.AddMemoryCache();
        services.AddTransient<AuthenticationDelegatingHandler>();
        services.AddTransient<HttpClientMetricsHandler>();

        // Register HttpClient for TokenService
        services.AddHttpClient<ITokenService, TokenService>((sp, client) =>
        {
            var rest = sp.GetRequiredService<IOptions<RestApiSettings>>().Value;
            if (!string.IsNullOrWhiteSpace(rest.BaseUrl))
            {
                client.BaseAddress = new Uri(rest.BaseUrl);
            }
        });

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
        })
        .AddHttpMessageHandler<AuthenticationDelegatingHandler>()
        .AddHttpMessageHandler<HttpClientMetricsHandler>();

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
        })
        .AddHttpMessageHandler<AuthenticationDelegatingHandler>()
        .AddHttpMessageHandler<HttpClientMetricsHandler>();

        return services;
    }
}
