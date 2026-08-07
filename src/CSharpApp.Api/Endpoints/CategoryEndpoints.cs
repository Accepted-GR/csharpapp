namespace CSharpApp.Api.Endpoints;

/// <summary>
/// Maps the category endpoints.
/// </summary>
public static class CategoryEndpoints
{
    public static IVersionedEndpointRouteBuilder MapCategoryEndpoints(
        this IVersionedEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("api/v{version:apiVersion}/categories")
            .WithTags("Categories")
            .HasApiVersion(1.0);

        group.MapGet(string.Empty, GetCategories)
            .WithName("GetCategories")
            .WithSummary("Returns all categories.");

        group.MapGet("{id:int}", GetCategoryById)
            .WithName("GetCategoryById")
            .WithSummary("Returns a single category.");

        group.MapPost(string.Empty, CreateCategory)
            .AddEndpointFilter<ValidationFilter<CategoryWriteModel>>()
            .WithName("CreateCategory")
            .WithSummary("Creates a category.");

        group.MapPut("{id:int}", UpdateCategory)
            .AddEndpointFilter<ValidationFilter<CategoryWriteModel>>()
            .WithName("UpdateCategory")
            .WithSummary("Replaces a category in full.");

        group.MapDelete("{id:int}", DeleteCategory)
            .WithName("DeleteCategory")
            .WithSummary("Deletes a category.");

        return builder;
    }

    private static async Task<Ok<IReadOnlyCollection<Category>>> GetCategories(
        ICategoriesService categories,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await categories.GetCategoriesAsync(cancellationToken));
    }

    private static async Task<Ok<Category>> GetCategoryById(
        int id,
        ICategoriesService categories,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await categories.GetCategoryByIdAsync(id, cancellationToken));
    }

    private static async Task<Created<Category>> CreateCategory(
        CategoryWriteModel body,
        ICategoriesService categories,
        CancellationToken cancellationToken)
    {
        var created = await categories.CreateCategoryAsync(body, cancellationToken);

        return TypedResults.Created($"/api/v1/categories/{created.Id}", created);
    }

    private static async Task<Ok<Category>> UpdateCategory(
        int id,
        CategoryWriteModel body,
        ICategoriesService categories,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await categories.UpdateCategoryAsync(id, body, cancellationToken));
    }

    private static async Task<NoContent> DeleteCategory(
        int id,
        ICategoriesService categories,
        CancellationToken cancellationToken)
    {
        await categories.DeleteCategoryAsync(id, cancellationToken);

        return TypedResults.NoContent();
    }
}
