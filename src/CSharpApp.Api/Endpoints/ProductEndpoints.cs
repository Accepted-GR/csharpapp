namespace CSharpApp.Api.Endpoints;

/// <summary>
/// Maps the product endpoints.
/// </summary>
/// <remarks>
/// Kept out of Program.cs deliberately. With products and categories both offering
/// full CRUD, inlining every endpoint would leave Program.cs around a dozen lambdas
/// long, which is where minimal APIs stop being readable.
/// </remarks>
public static class ProductEndpoints
{
    public static IVersionedEndpointRouteBuilder MapProductEndpoints(
        this IVersionedEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("api/v{version:apiVersion}/products")
            .WithTags("Products")
            .HasApiVersion(1.0);

        group.MapGet(string.Empty, GetProducts)
            .WithName("GetProducts")
            .WithSummary("Returns all products.");

        group.MapGet("{id:int}", GetProductById)
            .WithName("GetProductById")
            .WithSummary("Returns a single product.");

        group.MapPost(string.Empty, CreateProduct)
            .AddEndpointFilter<ValidationFilter<ProductWriteModel>>()
            .WithName("CreateProduct")
            .WithSummary("Creates a product.");

        group.MapPut("{id:int}", UpdateProduct)
            .AddEndpointFilter<ValidationFilter<ProductWriteModel>>()
            .WithName("UpdateProduct")
            .WithSummary("Replaces a product in full.");

        group.MapDelete("{id:int}", DeleteProduct)
            .WithName("DeleteProduct")
            .WithSummary("Deletes a product.");

        return builder;
    }

    private static async Task<Ok<IReadOnlyCollection<Product>>> GetProducts(
        IProductsService products,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await products.GetProductsAsync(cancellationToken));
    }

    private static async Task<Ok<Product>> GetProductById(
        int id,
        IProductsService products,
        CancellationToken cancellationToken)
    {
        // A missing product surfaces as UpstreamNotFoundException and becomes a 404
        // ProblemDetails in the exception handler, so there is no null check to
        // forget here.
        return TypedResults.Ok(await products.GetProductByIdAsync(id, cancellationToken));
    }

    private static async Task<Created<Product>> CreateProduct(
        ProductWriteModel body,
        IProductsService products,
        CancellationToken cancellationToken)
    {
        var created = await products.CreateProductAsync(body, cancellationToken);

        // 201 with a Location header, rather than the 200 upstream returns.
        return TypedResults.Created($"/api/v1/products/{created.Id}", created);
    }

    private static async Task<Ok<Product>> UpdateProduct(
        int id,
        ProductWriteModel body,
        IProductsService products,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await products.UpdateProductAsync(id, body, cancellationToken));
    }

    private static async Task<NoContent> DeleteProduct(
        int id,
        IProductsService products,
        CancellationToken cancellationToken)
    {
        await products.DeleteProductAsync(id, cancellationToken);

        // 204, where upstream answers a delete with 200 and a bare "true".
        return TypedResults.NoContent();
    }
}
