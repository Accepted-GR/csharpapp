using CSharpApp.Application.Categories.Commands;
using CSharpApp.Application.Categories.Queries;
using CSharpApp.Application.Products.Commands;
using CSharpApp.Application.Products.Queries;
using CSharpApp.Core.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/getproducts", async (IMediator mediator, CancellationToken cancellationToken) =>
	{
		var products = await mediator.Send(new GetProductsQuery(), cancellationToken);
		return Results.Ok(products);
	})
	.WithGroupName("Products")
	.WithName("GetProducts")
	.HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/products/{id}", async (int id, IMediator mediator, CancellationToken cancellationToken) =>
{
	var response = await mediator.Send(new GetProductByIdQuery(id), cancellationToken);

	return response.Product is null
		? Results.NotFound(id)
		: Results.Ok(response);
})
	.WithGroupName("Products")
	.WithName("GetProduct")
	.HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/products", async ([FromBody] CreateProductPayload payload, IMediator mediator, CancellationToken cancellationToken) =>
{
	var productId = await mediator.Send(new CreateProductCommand(payload), cancellationToken);

	return productId is null
		? Results.BadRequest()
		: Results.Created();
})
	.WithGroupName("Products")
	.WithName("CreateProduct")
	.HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/getcategories", async (IMediator mediator, CancellationToken cancellationToken) =>
	{
		var categories = await mediator.Send(new GetCategoriesQuery(), cancellationToken);
		return Results.Ok(categories);
	})
	.WithGroupName("Categories")
	.WithName("GetCategories")
	.HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/categories/{id}", async (int id, IMediator mediator, CancellationToken cancellationToken) =>
{
	var response = await mediator.Send(new GetCategoryByIdQuery(id), cancellationToken);

	return response.Category is null
		? Results.NotFound(id)
		: Results.Ok(response);
})
	.WithGroupName("Categories")
	.WithName("GetCategory")
	.HasApiVersion(1.0);

versionedEndpointRouteBuilder.MapPost("api/v{version:apiVersion}/categories", async ([FromBody] CreateCategoryPayload payload, IMediator mediator, CancellationToken cancellationToken) =>
{
	var categoryId = await mediator.Send(new CreateCategoryCommand(payload), cancellationToken);

	return categoryId is null
		? Results.BadRequest()
		: Results.Created();
})
	.WithGroupName("Categories")
	.WithName("CreateCategory")
	.HasApiVersion(1.0);

app.Run();