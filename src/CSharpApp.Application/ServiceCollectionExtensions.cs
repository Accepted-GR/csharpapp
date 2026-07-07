using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpApp.Application
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register MediatR with all handlers in this assembly
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            // Application-level validators, mappers and other services
            services.AddSingleton<Products.IProductValidator, Products.ProductBusinessValidator>();
            services.AddSingleton<Products.IProductMapper, Products.ProductMapper>();
            // Categories (moved to structured folders)
            services.AddSingleton<Categories.Validation.ICategoryValidator, Categories.Validation.CategoryValidator>();
            services.AddSingleton<Categories.Mapping.ICategoryMapper, Categories.Mapping.CategoryMapper>();
            // Upstream category mapper interface will be implemented by Infrastructure
            // We do not register it here to allow Infrastructure to provide its implementation.

            return services;
        }
    }
}
