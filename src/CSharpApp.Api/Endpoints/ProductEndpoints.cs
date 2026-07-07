using MediatR;
using CSharpApp.Core.Dtos;
using CSharpApp.Application.Products.Queries;
using CSharpApp.Application.Products.Commands;
using CSharpApp.Application.Products;
using Asp.Versioning.Builder;

namespace CSharpApp.Api.Endpoints;

public static class ProductEndpoints
{
    public static IVersionedEndpointRouteBuilder MapProductEndpoints(this IVersionedEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/v{version:apiVersion}/products")
            .HasApiVersion(1.0);

        group.MapGet("/", GetProducts)
            .WithName("GetProducts");

        group.MapGet("/{id}", GetProductById)
            .WithName("GetProductById");

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct");

        return routes;
    }

    private static async Task<IResult> GetProducts(
        IMediator mediator,
        int? offset,
        int? limit)
    {
        var products = await mediator.Send(new GetProductsQuery(offset, limit));
        return Results.Ok(products);
    }

    private static async Task<IResult> GetProductById(
        IMediator mediator,
        int id,
        CancellationToken ct)
    {
        var product = await mediator.Send(new GetProductByIdQuery(id), ct);
        return product is null ? Results.NotFound() : Results.Ok(product);
    }

    private static async Task<IResult> CreateProduct(
        IMediator mediator,
        IProductMapper mapper,
        CreateProductRequestDto request,
        CancellationToken ct)
    {
        if (request == null)
            return Results.BadRequest(new { errors = new[] { "Request cannot be null" } });

        try
        {
            // Map request to domain product using mapper
            var product = mapper.MapFromCreateRequest(request);

            // MediatR pipeline will automatically validate via FluentValidation behavior
            var created = await mediator.Send(new CreateProductCommand(product), ct);
            if (created == null)
                return Results.StatusCode(StatusCodes.Status502BadGateway);

            var location = $"/api/v1/products/{created.Id}";
            return Results.Created(location, created);
        }
        catch (FluentValidation.ValidationException ex)
        {
            var errors = ex.Errors.Select(e => e.ErrorMessage).ToList();
            return Results.BadRequest(new { errors });
        }
    }
}
