namespace CSharpApp.Application.Categories;

/// <summary>
/// Typed client over the upstream categories endpoints.
/// </summary>
/// <remarks>
/// Mirrors <see cref="Products.ProductsService"/> deliberately. The two resources
/// behave the same way from our side even though upstream is inconsistent about
/// them -- notably, its category PUT tolerates a partial body while its product PUT
/// returns a 500 for one.
/// </remarks>
public sealed partial class CategoriesService : ICategoriesService
{
    private const string EntityName = "Category";

    private readonly HttpClient _httpClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<CategoriesService> _logger;

    public CategoriesService(
        HttpClient httpClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<CategoriesService> logger)
    {
        _httpClient = httpClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    private string BasePath => _restApiSettings.Categories!;

    public async Task<IReadOnlyCollection<Category>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        LogRequestingCategories(BasePath);

        var categories = await _httpClient.GetFromJsonAsync<List<Category>>(BasePath, cancellationToken);

        if (categories is null)
        {
            LogNullPayload(BasePath);
            return [];
        }

        LogCategoriesRetrieved(categories.Count);

        return categories;
    }

    public async Task<Category> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"{BasePath}/{id}", cancellationToken);

        await UpstreamResponse.EnsureFoundAsync(response, EntityName, id, cancellationToken);

        return await UpstreamResponse.ReadAsync<Category>(response, cancellationToken);
    }

    public async Task<Category> CreateCategoryAsync(
        CategoryWriteModel category,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(BasePath, category, cancellationToken);

        var created = await UpstreamResponse.ReadAsync<Category>(response, cancellationToken);

        LogCategoryCreated(created.Id ?? 0);

        return created;
    }

    public async Task<Category> UpdateCategoryAsync(
        int id,
        CategoryWriteModel category,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"{BasePath}/{id}", category, cancellationToken);

        await UpstreamResponse.EnsureFoundAsync(response, EntityName, id, cancellationToken);

        var updated = await UpstreamResponse.ReadAsync<Category>(response, cancellationToken);

        LogCategoryUpdated(id);

        return updated;
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"{BasePath}/{id}", cancellationToken);

        await UpstreamResponse.EnsureFoundAsync(response, EntityName, id, cancellationToken);

        response.EnsureSuccessStatusCode();

        LogCategoryDeleted(id);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Requesting all categories from {Path}")]
    private partial void LogRequestingCategories(string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Upstream returned a null payload for {Path}")]
    private partial void LogNullPayload(string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved {Count} categories")]
    private partial void LogCategoriesRetrieved(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created category {CategoryId}")]
    private partial void LogCategoryCreated(int categoryId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated category {CategoryId}")]
    private partial void LogCategoryUpdated(int categoryId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted category {CategoryId}")]
    private partial void LogCategoryDeleted(int categoryId);
}
