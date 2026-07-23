using System.Net.Http.Json;

namespace CSharpApp.Application.Categories;

public class CategoriesService : ICategoriesService
{
	private readonly HttpClient _httpClient;
	private readonly JsonSerializerOptions _jsonOptions;
	private readonly RestApiSettings _restApiSettings;
	private readonly ILogger<CategoriesService> _logger;

	public CategoriesService(HttpClient httpClient, IOptions<RestApiSettings> restApiSettings,
		ILogger<CategoriesService> logger)
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

	public async Task<int?> CreateCategory(CreateCategoryPayload payload, CancellationToken cancellationToken)
	{
		try
		{
			var response = await _httpClient.PostAsJsonAsync(_restApiSettings.Categories, payload, _jsonOptions, cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("An unexpected error occured during the external http client call with status {Status}: {Body}", response.StatusCode, errorContent);

				return null;
			}

			var content = await response.Content.ReadAsStringAsync();
			var res = JsonSerializer.Deserialize<Category>(content);

			return res?.Id;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An unexpected error occured during the external http client call.");
			return null;
		}
	}

	public async Task<Category?> GetCategoryById(int id, CancellationToken cancellationToken)
	{
		try
		{
			var response = await _httpClient.GetAsync($"{_restApiSettings.Categories}/{id}", cancellationToken);
			response.EnsureSuccessStatusCode();

			var content = await response.Content.ReadAsStringAsync();
			var res = JsonSerializer.Deserialize<Category>(content);

			return res;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An unexpected error occured during the external http client call.");
			return null;
		}
	}

	public async Task<IReadOnlyCollection<Category>> GetCategories(CancellationToken cancellationToken)
	{
		try
		{
			var response = await _httpClient.GetAsync(_restApiSettings.Categories, cancellationToken);
			response.EnsureSuccessStatusCode();

			var content = await response.Content.ReadAsStringAsync();
			var res = JsonSerializer.Deserialize<Category[]>(content);

			return res ?? Array.Empty<Category>();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An unexpected error occured during the external http client call.");
			return Array.Empty<Category>();
		}
	}
}
