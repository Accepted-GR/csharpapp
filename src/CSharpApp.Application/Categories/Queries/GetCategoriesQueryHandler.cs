using MediatR;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Settings;

namespace CSharpApp.Application.Categories.Queries;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyCollection<Category>>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<GetCategoriesQueryHandler> _logger;

    public GetCategoriesQueryHandler(
        IHttpClientFactory httpClientFactory,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<GetCategoriesQueryHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<Category>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("RestApiClient");
            var path = string.IsNullOrWhiteSpace(_restApiSettings.Categories) ? string.Empty : _restApiSettings.Categories;

            var response = await httpClient.GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var categories = JsonSerializer.Deserialize<List<Category>>(content, options) ?? new List<Category>();

            return categories.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching categories from remote API");
            throw;
        }
    }
}
