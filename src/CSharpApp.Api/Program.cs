using CSharpApp.Core.Dtos;
using CSharpApp.Application;
using CSharpApp.Infrastructure;
using CSharpApp.Api;
using CSharpApp.Api.Validation;

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

// Register application and infrastructure services via extension helpers
builder.Services.AddApiServices();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

// Performance logging middleware
app.UseMiddleware<CSharpApp.Api.Middleware.PerformanceLoggingMiddleware>();

var versionedEndpointRouteBuilder = app.NewVersionedApi();

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/products", async (IProductsService productsService, int? offset, int? limit) =>
    {
        var products = await productsService.GetProducts(offset, limit);
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

versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/products", async (IProductsService productsService, CSharpApp.Api.Validation.ICreateProductValidator apiValidator, CSharpApp.Application.Products.IProductValidator validator, CSharpApp.Application.Products.IProductMapper mapper, CreateProductRequestDto request, HttpContext http, CancellationToken ct) =>
    {
        if (request == null)
            return Results.BadRequest();

        // API-level validation (shape/format)
        if (!apiValidator.Validate(request, out var apiErrors))
            return Results.BadRequest(new { errors = apiErrors });

        // Application/business validation
        var validation = validator.ValidateForCreate(request);
        if (!validation.IsValid)
            return Results.BadRequest(new { errors = validation.Errors });

        // Map request to domain product using mapper
        var product = mapper.MapFromCreateRequest(request);

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

versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/categories", async (ICategoriesService categoriesService, CSharpApp.Api.Validation.ICreateCategoryValidator apiCategoryValidator, CSharpApp.Application.Categories.Validation.ICategoryValidator categoryValidator, CSharpApp.Application.Categories.Mapping.ICategoryMapper mapper, CSharpApp.Core.Dtos.CreateCategoryRequestDto request, CancellationToken ct) =>
    {
        if (request == null)
            return Results.BadRequest();

        // API-level validation
        if (!apiCategoryValidator.Validate(request, out var apiErrors))
            return Results.BadRequest(new { errors = apiErrors });

        // Business validation
        var businessValidation = categoryValidator.ValidateForCreate(request);
        if (!businessValidation.IsValid)
            return Results.BadRequest(new { errors = businessValidation.Errors });

        var category = mapper.MapFromCreateRequest(request);

        var created = await categoriesService.CreateCategory(category, ct);
        if (created == null)
            return Results.StatusCode(StatusCodes.Status502BadGateway);

        var location = $"/api/v1/categories/{created.Id}";
        return Results.Created(location, created);
    })
    .WithName("CreateCategory")
    .HasApiVersion(1.0);

app.Run();