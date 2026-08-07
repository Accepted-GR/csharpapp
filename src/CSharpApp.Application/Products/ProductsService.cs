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
    private const string EntityName = "Product";

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

    private string BasePath => _restApiSettings.Products!;

    public async Task<IReadOnlyCollection<Product>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        LogRequestingProducts(BasePath);

        var products = await _httpClient.GetFromJsonAsync<List<Product>>(BasePath, cancellationToken);

        if (products is null)
        {
            // A literal "null" body is well-formed JSON but not a usable result.
            // Guarding here is what removes the CS8602 from the original code
            // (finding #7) rather than suppressing it.
            LogNullPayload(BasePath);
            return [];
        }

        LogProductsRetrieved(products.Count);

        return products;
    }

    public async Task<Product> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"{BasePath}/{id}", cancellationToken);

        await UpstreamResponse.EnsureFoundAsync(response, EntityName, id, cancellationToken);

        return await UpstreamResponse.ReadAsync<Product>(response, cancellationToken);
    }

    public async Task<Product> CreateProductAsync(
        ProductWriteModel product,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(BasePath, product, cancellationToken);

        // A create can 404 too: upstream rejects an unknown categoryId with the same
        // EntityNotFoundError shape it uses for a missing product.
        await UpstreamResponse.EnsureFoundAsync(
            response, "Category", product.CategoryId ?? 0, cancellationToken);

        var created = await UpstreamResponse.ReadAsync<Product>(response, cancellationToken);

        LogProductCreated(created.Id ?? 0);

        return created;
    }

    public async Task<Product> UpdateProductAsync(
        int id,
        ProductWriteModel product,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"{BasePath}/{id}", product, cancellationToken);

        await UpstreamResponse.EnsureFoundAsync(response, EntityName, id, cancellationToken);

        var updated = await UpstreamResponse.ReadAsync<Product>(response, cancellationToken);

        LogProductUpdated(id);

        return updated;
    }

    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"{BasePath}/{id}", cancellationToken);

        await UpstreamResponse.EnsureFoundAsync(response, EntityName, id, cancellationToken);

        response.EnsureSuccessStatusCode();

        LogProductDeleted(id);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Created product {ProductId}")]
    private partial void LogProductCreated(int productId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated product {ProductId}")]
    private partial void LogProductUpdated(int productId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted product {ProductId}")]
    private partial void LogProductDeleted(int productId);
}
