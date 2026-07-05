var builder = WebApplication.CreateBuilder(args);

var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Logging.ClearProviders().AddSerilog(logger);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDefaultConfiguration(builder.Configuration);
builder.Services.AddHttpConfiguration(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddApiVersioning();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

var versionedEndpointRouteBuilder = app.NewVersionedApi();

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/products", async (IProductsService productsService) =>
    {
        var products = await productsService.GetProducts();
        return Results.Ok(products);
    })
    .WithName("GetProducts")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/products/{id}", async (IProductsService productsService, int id, CancellationToken ct) =>
    {
        var product = await productsService.GetProductById(id, ct);
        return product is null ? Results.NotFound() : Results.Ok(product);
    })
    .WithName("GetProductById")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/products", async (IProductsService productsService, Product product, HttpContext http, CancellationToken ct) =>
    {
        if (product == null)
            return Results.BadRequest();

        // Basic validation
        if (string.IsNullOrWhiteSpace(product.Title) || (product.Price.HasValue && product.Price <= 0))
            return Results.BadRequest("Invalid product payload");

        var created = await productsService.CreateProduct(product, ct);
        if (created == null)
            return Results.StatusCode(StatusCodes.Status502BadGateway);

        var location = $"/api/v1/products/{created.Id}";
        return Results.Created(location, created);
    })
    .WithName("CreateProduct")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/categories", async (ICategoriesService categoriesService) =>
    {
        var categories = await categoriesService.GetCategories();
        return Results.Ok(categories);
    })
    .WithName("GetCategories")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/categories/{id}", async (ICategoriesService categoriesService, int id, CancellationToken ct) =>
    {
        var category = await categoriesService.GetCategoryById(id, ct);
        return category is null ? Results.NotFound() : Results.Ok(category);
    })
    .WithName("GetCategoryById")
    .HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/categories", async (ICategoriesService categoriesService, Category category, HttpContext http, CancellationToken ct) =>
    {
        if (category == null)
            return Results.BadRequest();

        // Basic validation
        if (string.IsNullOrWhiteSpace(category.Name))
            return Results.BadRequest("Invalid category payload");

        var created = await categoriesService.CreateCategory(category, ct);
        if (created == null)
            return Results.StatusCode(StatusCodes.Status502BadGateway);

        var location = $"/api/v1/categories/{created.Id}";
        return Results.Created(location, created);
    })
    .WithName("CreateCategory")
    .HasApiVersion(1.0);

app.Run();