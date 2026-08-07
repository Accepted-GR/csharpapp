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

// ProblemDetails was registered but never activated, so nothing translated an
// exception into a payload. See finding #6 in docs/AUDIT.md.
builder.Services.AddExceptionHandler<UpstreamExceptionHandler>();
builder.Services.AddSingleton<RequestPerformanceMiddleware>();

var app = builder.Build();

// Configure the HTTP request pipeline.

// First in the pipeline, so the measurement covers everything after it --
// including the exception handler. Measuring only the successful path would miss
// the timings most worth knowing about.
app.UseMiddleware<RequestPerformanceMiddleware>();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

var versionedEndpointRouteBuilder = app.NewVersionedApi();

versionedEndpointRouteBuilder.MapProductEndpoints();
versionedEndpointRouteBuilder.MapCategoryEndpoints();

// Exercises the full JWT pipeline: the bearer handler acquires and attaches a
// token, and upstream rejects the call without one. The product write endpoints
// cannot demonstrate this -- upstream does not enforce auth on them.
versionedEndpointRouteBuilder.MapGet("api/v{version:apiVersion}/auth/profile",
        async (IProfileService profileService, CancellationToken cancellationToken) =>
            await profileService.GetProfileAsync(cancellationToken))
    .WithName("GetProfile")
    .HasApiVersion(1.0);

app.Run();