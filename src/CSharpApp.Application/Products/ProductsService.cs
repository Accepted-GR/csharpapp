namespace CSharpApp.Application.Products;

public class ProductsService : IProductsService
{
    private readonly HttpClient _httpClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<ProductsService> _logger;
    private readonly IUpstreamProductMapper? _upstreamMapper;

    public ProductsService(HttpClient httpClient, IOptions<RestApiSettings> restApiSettings,
        ILogger<ProductsService> logger, IUpstreamProductMapper? upstreamMapper = null)
    {
        _httpClient = httpClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
        _upstreamMapper = upstreamMapper;
    }

    public async Task<Product?> GetProductById(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var path = string.IsNullOrWhiteSpace(_restApiSettings.Products) ? $"{id}" : $"{_restApiSettings.Products}/{id}";

            var response = await _httpClient.GetAsync(path, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var product = JsonSerializer.Deserialize<Product>(content, options);

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId} from remote API", id);
            throw;
        }
    }

    public async Task<Product?> CreateProduct(Product product, CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic validation
            if (product is null)
                throw new ArgumentNullException(nameof(product));

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            // Use infrastructure mapper when available by resolving IUpstreamProductMapper from DI via HttpClient's service provider
            // Fallback to local mapping if mapper not available
            UpstreamCreateProduct upstream;
            if (_upstreamMapper != null)
                upstream = _upstreamMapper.Map(product);
            else
                upstream = new UpstreamCreateProduct
                {
                    Title = product.Title,
                    Price = product.Price,
                    Description = product.Description,
                    Images = product.Images != null && product.Images.Count > 0 ? new List<string>(product.Images) : null,
                    CategoryId = product.Category?.Id
                };

            var json = JsonSerializer.Serialize(upstream, options);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var path = string.IsNullOrWhiteSpace(_restApiSettings.Products) ? string.Empty : _restApiSettings.Products;

            var response = await _httpClient.PostAsync(path, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to create product. StatusCode: {StatusCode}", response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var created = JsonSerializer.Deserialize<Product>(responseContent, options);

            return created ?? product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            throw;
        }
    }

    public async Task<IReadOnlyCollection<Product>> GetProducts(int? offset = null, int? limit = null)
    {
        try
        {
            // If Products path is set, request it; otherwise request root
            var path = string.IsNullOrWhiteSpace(_restApiSettings.Products) ? string.Empty : _restApiSettings.Products;

            // Append query parameters for upstream pagination when provided
            if (offset.HasValue || limit.HasValue)
            {
                var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
                if (offset.HasValue) query["offset"] = offset.Value.ToString();
                if (limit.HasValue) query["limit"] = limit.Value.ToString();

                var qs = query.ToString();
                if (!string.IsNullOrEmpty(qs)) path = string.IsNullOrEmpty(path) ? "?" + qs : path + "?" + qs;
            }

            var response = await _httpClient.GetAsync(path);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var res = JsonSerializer.Deserialize<List<Product>>(content, options) ?? new List<Product>();

            return res.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from remote API");
            throw;
        }
    }
}