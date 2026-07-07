using Microsoft.Extensions.DependencyInjection;

namespace CSharpApp.Api
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            // API-level validators and request/adapters
            services.AddSingleton<Validation.ICreateProductValidator, Validation.CreateProductValidator>();
            services.AddSingleton<Validation.ICreateCategoryValidator, Validation.CreateCategoryValidator>();

            return services;
        }
    }
}
