using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using CSharpApp.Application.Products.Queries;
using CSharpApp.Application.Products.Commands;
using CSharpApp.Application.Categories.Queries;
using CSharpApp.Application.Categories.Commands;
using CSharpApp.Core.Dtos;

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

            // Override MediatR's handler registrations to ensure HttpClient-configured handlers are used
            // These registrations must come AFTER AddApplicationServices (which registers MediatR)
            services.AddTransient<IRequestHandler<GetProductsQuery, IReadOnlyCollection<Product>>, GetProductsQueryHandler>();
            services.AddTransient<IRequestHandler<GetProductByIdQuery, Product?>, GetProductByIdQueryHandler>();
            services.AddTransient<IRequestHandler<GetCategoriesQuery, IReadOnlyCollection<Category>>, GetCategoriesQueryHandler>();
            services.AddTransient<IRequestHandler<GetCategoryByIdQuery, Category?>, GetCategoryByIdQueryHandler>();
            services.AddTransient<IRequestHandler<CreateProductCommand, Product?>, CreateProductCommandHandler>();
            services.AddTransient<IRequestHandler<CreateCategoryCommand, Category?>, CreateCategoryCommandHandler>();

            return services;
        }
    }
}
