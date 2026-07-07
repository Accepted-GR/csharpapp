using Microsoft.Extensions.DependencyInjection;

namespace CSharpApp.Api
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            // API-level services can be registered here if needed in the future
            return services;
        }
    }
}
