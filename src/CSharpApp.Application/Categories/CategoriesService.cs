namespace CSharpApp.Application.Categories;

public class CategoriesService : ICategoriesService
{
    private readonly HttpClient _httpClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<CategoriesService> _logger;

    public CategoriesService(HttpClient httpClient, IOptions<RestApiSettings> restApiSettings,
        ILogger<CategoriesService> logger)
    {
        _httpClient = httpClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<Category>> GetCategories(CancellationToken cancellationToken = default)
    {
        try
        {
            var path = string.IsNullOrWhiteSpace(_restApiSettings.Categories) ? string.Empty : _restApiSettings.Categories;

            var response = await _httpClient.GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var res = JsonSerializer.Deserialize<List<Category>>(content, options) ?? new List<Category>();

            return res.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching categories from remote API");
            throw;
        }
    }

    public async Task<Category?> GetCategoryById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var path = string.IsNullOrWhiteSpace(_restApiSettings.Categories) ? $"{id}" : $"{_restApiSettings.Categories}/{id}";

            var response = await _httpClient.GetAsync(path, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var category = JsonSerializer.Deserialize<Category>(content, options);

            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching category {CategoryId} from remote API", id);
            throw;
        }
    }

    public async Task<Category?> CreateCategory(Category category, CancellationToken cancellationToken = default)
    {
        try
        {
            if (category is null)
                throw new ArgumentNullException(nameof(category));

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var json = JsonSerializer.Serialize(category, options);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var path = string.IsNullOrWhiteSpace(_restApiSettings.Categories) ? string.Empty : _restApiSettings.Categories;

            var response = await _httpClient.PostAsync(path, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to create category. StatusCode: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var created = JsonSerializer.Deserialize<Category>(responseContent, options);

            return created ?? category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            throw;
        }
    }
}
