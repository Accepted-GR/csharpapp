using MediatR;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Settings;

namespace CSharpApp.Application.Categories.Queries;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Category?>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<GetCategoryByIdQueryHandler> _logger;

    public GetCategoryByIdQueryHandler(
        IHttpClientFactory httpClientFactory,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<GetCategoryByIdQueryHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<Category?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("RestApiClient");
            var path = string.IsNullOrWhiteSpace(_restApiSettings.Categories) 
                ? $"{request.Id}" 
                : $"{_restApiSettings.Categories}/{request.Id}";

            var response = await httpClient.GetAsync(path, cancellationToken);

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
            _logger.LogError(ex, "Error fetching category {CategoryId} from remote API", request.Id);
            throw;
        }
    }
}
