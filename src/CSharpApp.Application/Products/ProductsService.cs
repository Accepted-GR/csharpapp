namespace CSharpApp.Application.Products;

public class ProductsService : IProductsService
{
	private readonly HttpClient _httpClient;
	private readonly RestApiSettings _restApiSettings;
	private readonly ILogger<ProductsService> _logger;

	public ProductsService(HttpClient httpClient, IOptions<RestApiSettings> restApiSettings,
		ILogger<ProductsService> logger)
	{
		_httpClient = httpClient;
		_restApiSettings = restApiSettings.Value;
		_logger = logger;
	}

	public async Task<IReadOnlyCollection<Product>> GetProducts()
	{
		try
		{
			_httpClient.BaseAddress = new Uri(_restApiSettings.BaseUrl!);

			var response = await _httpClient.GetAsync(_restApiSettings.Products);
			response.EnsureSuccessStatusCode();

			var content = await response.Content.ReadAsStringAsync();
			var res = JsonSerializer.Deserialize<Product[]>(content);

			return res ?? Array.Empty<Product>();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An unexpected error occured during the external http client call.");
			return Array.Empty<Product>();
		}
	}
}