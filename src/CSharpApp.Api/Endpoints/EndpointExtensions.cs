using Asp.Versioning.Builder;

namespace CSharpApp.Api.Endpoints;

public static class EndpointExtensions
{
    /// <summary>
    /// Registers all versioned API endpoints.
    /// </summary>
    public static IVersionedEndpointRouteBuilder MapVersionedEndpoints(this IVersionedEndpointRouteBuilder routes)
    {
        routes.MapProductEndpoints();
        routes.MapCategoryEndpoints();

        return routes;
    }
}
