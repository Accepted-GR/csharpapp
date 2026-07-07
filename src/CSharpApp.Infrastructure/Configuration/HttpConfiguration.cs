using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MediatR;
using CSharpApp.Application.Categories;
using CSharpApp.Application.Products;
using CSharpApp.Application.Products.Queries;
using CSharpApp.Application.Products.Commands;
using CSharpApp.Application.Categories.Queries;
using CSharpApp.Application.Categories.Commands;
using CSharpApp.Core.Dtos;
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

        // Register named HttpClient for all CQRS handlers
        services.AddHttpClient("RestApiClient", (sp, client) =>
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
