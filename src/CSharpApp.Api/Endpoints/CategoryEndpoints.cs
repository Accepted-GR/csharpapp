using MediatR;
using CSharpApp.Core.Dtos;
using CSharpApp.Application.Categories.Queries;
using CSharpApp.Application.Categories.Commands;
using CSharpApp.Application.Categories.Mapping;
using Asp.Versioning.Builder;

namespace CSharpApp.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IVersionedEndpointRouteBuilder MapCategoryEndpoints(this IVersionedEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/v{version:apiVersion}/categories")
            .HasApiVersion(1.0);

        group.MapGet("/", GetCategories)
            .WithName("GetCategories");

        group.MapGet("/{id}", GetCategoryById)
            .WithName("GetCategoryById");

        group.MapPost("/", CreateCategory)
            .WithName("CreateCategory");

        return routes;
    }

    private static async Task<IResult> GetCategories(IMediator mediator)
    {
        var categories = await mediator.Send(new GetCategoriesQuery());
        return Results.Ok(categories);
    }

    private static async Task<IResult> GetCategoryById(
        IMediator mediator,
        int id,
        CancellationToken ct)
    {
        var category = await mediator.Send(new GetCategoryByIdQuery(id), ct);
        return category is null ? Results.NotFound() : Results.Ok(category);
    }

    private static async Task<IResult> CreateCategory(
        IMediator mediator,
        ICategoryMapper mapper,
        CreateCategoryRequestDto request,
        CancellationToken ct)
    {
        if (request == null)
            return Results.BadRequest(new { errors = new[] { "Request cannot be null" } });

        var category = mapper.MapFromCreateRequest(request);

        // MediatR pipeline will automatically validate via FluentValidation behavior
        // ValidationException will be caught by GlobalExceptionHandlerMiddleware
        var created = await mediator.Send(new CreateCategoryCommand(category), ct);
        if (created == null)
            return Results.StatusCode(StatusCodes.Status502BadGateway);

        var location = $"/api/v1/categories/{created.Id}";
        return Results.Created(location, created);
    }
}
