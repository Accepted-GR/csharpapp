using System.Net.Http.Json;

namespace CSharpApp.Application.Products;

public class ProductsService : IProductsService
{
	private readonly HttpClient _httpClient;
	private readonly JsonSerializerOptions _jsonOptions;
	private readonly RestApiSettings _restApiSettings;
	private readonly ILogger<ProductsService> _logger;

	public ProductsService(HttpClient httpClient, IOptions<RestApiSettings> restApiSettings,
		ILogger<ProductsService> logger)
	{
		_httpClient = httpClient;
		_restApiSettings = restApiSettings.Value;
		_logger = logger;

		_httpClient.BaseAddress = new Uri(_restApiSettings.BaseUrl!);

		_jsonOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
	}

	public async Task<int?> CreateProduct(CreateProductPayload payload, CancellationToken cancellationToken)
	{
		try
		{
			var response = await _httpClient.PostAsJsonAsync(_restApiSettings.Products, payload, _jsonOptions, cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("An unexpected error occured during the external http client call with status {Status}: {Body}", response.StatusCode, errorContent);

				return null;
			}

			var content = await response.Content.ReadAsStringAsync();
			var res = JsonSerializer.Deserialize<Product>(content);

			return res?.Id;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An unexpected error occured during the external http client call.");
			return null;
		}
	}

	public async Task<Product?> GetProductById(int id, CancellationToken cancellationToken)
	{
		try
		{
			var response = await _httpClient.GetAsync($"{_restApiSettings.Products}/{id}", cancellationToken);
			response.EnsureSuccessStatusCode();

			var content = await response.Content.ReadAsStringAsync();
			var res = JsonSerializer.Deserialize<Product>(content);

			return res;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An unexpected error occured during the external http client call.");
			return null;
		}
	}

	public async Task<IReadOnlyCollection<Product>> GetProducts(CancellationToken cancellationToken)
	{
		try
		{
			var response = await _httpClient.GetAsync(_restApiSettings.Products, cancellationToken);
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