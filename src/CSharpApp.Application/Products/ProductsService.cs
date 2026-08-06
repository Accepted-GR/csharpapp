namespace CSharpApp.Application.Products;

/// <summary>
/// Typed client over the upstream products endpoints.
/// </summary>
/// <remarks>
/// The <see cref="HttpClient"/> is injected and owned by
/// <see cref="IHttpClientFactory"/>. This type must not dispose it and must not
/// mutate its configuration -- both are the factory's responsibility, set once at
/// registration time in <c>HttpConfiguration</c>. That is what resolves findings
/// #1 and #8 in docs/AUDIT.md.
/// </remarks>
public sealed partial class ProductsService : IProductsService
{
    private readonly HttpClient _httpClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<ProductsService> _logger;

    public ProductsService(
        HttpClient httpClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<ProductsService> logger)
    {
        _httpClient = httpClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<Product>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var path = _restApiSettings.Products!;

        LogRequestingProducts(path);

        var products = await _httpClient.GetFromJsonAsync<List<Product>>(path, cancellationToken);

        if (products is null)
        {
            // A literal "null" body is well-formed JSON but not a usable result.
            // Guarding here is what removes the CS8602 from the original code
            // (finding #7) rather than suppressing it.
            LogNullPayload(path);
            return [];
        }

        LogProductsRetrieved(products.Count);

        return products;
    }

    // Source-generated logging. The generator emits a cached delegate and an
    // IsEnabled check per message, so a disabled level costs nothing and the
    // structured property names stay consistent across every call site.
    [LoggerMessage(Level = LogLevel.Debug, Message = "Requesting all products from {Path}")]
    private partial void LogRequestingProducts(string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Upstream returned a null payload for {Path}")]
    private partial void LogNullPayload(string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved {Count} products")]
    private partial void LogProductsRetrieved(int count);
}
