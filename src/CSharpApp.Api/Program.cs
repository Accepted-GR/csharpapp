using CSharpApp.Application;
using CSharpApp.Infrastructure;
using CSharpApp.Api;
using CSharpApp.Api.Endpoints;

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

// Global exception handling middleware (must be early in pipeline)
app.UseMiddleware<CSharpApp.Api.Middleware.GlobalExceptionHandlerMiddleware>();

// Performance logging middleware
app.UseMiddleware<CSharpApp.Api.Middleware.PerformanceLoggingMiddleware>();

// Register all versioned API endpoints
var versionedApi = app.NewVersionedApi();
versionedApi.MapVersionedEndpoints();

app.Run();