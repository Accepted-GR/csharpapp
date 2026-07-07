using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Settings;

namespace CSharpApp.Application.Categories.Queries;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Category?>
{
    private readonly HttpClient _httpClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<GetCategoryByIdQueryHandler> _logger;

    public GetCategoryByIdQueryHandler(
        HttpClient httpClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<GetCategoryByIdQueryHandler> logger)
    {
        _httpClient = httpClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<Category?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var path = string.IsNullOrWhiteSpace(_restApiSettings.Categories) 
                ? $"{request.Id}" 
                : $"{_restApiSettings.Categories}/{request.Id}";

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
            _logger.LogError(ex, "Error fetching category {CategoryId} from remote API", request.Id);
            throw;
        }
    }
}
