using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpApp.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Infrastructure adapters, clients and other registrations
            services.AddSingleton<CSharpApp.Application.Products.IUpstreamProductMapper, Adapters.UpstreamProductMapper>();
            // Upstream category mapper
            services.AddSingleton<CSharpApp.Application.Categories.IUpstreamCategoryMapper, Adapters.UpstreamCategoryMapper>();

            return services;
        }
    }
}
