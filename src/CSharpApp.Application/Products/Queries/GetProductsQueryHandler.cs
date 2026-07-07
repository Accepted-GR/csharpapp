using MediatR;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Settings;

namespace CSharpApp.Application.Products.Queries;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IReadOnlyCollection<Product>>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    public GetProductsQueryHandler(
        IHttpClientFactory httpClientFactory,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<GetProductsQueryHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<Product>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("RestApiClient");
            var path = string.IsNullOrWhiteSpace(_restApiSettings.Products) ? string.Empty : _restApiSettings.Products;

            if (request.Offset.HasValue || request.Limit.HasValue)
            {
                var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
                if (request.Offset.HasValue) query["offset"] = request.Offset.Value.ToString();
                if (request.Limit.HasValue) query["limit"] = request.Limit.Value.ToString();

                var qs = query.ToString();
                if (!string.IsNullOrEmpty(qs)) path = string.IsNullOrEmpty(path) ? "?" + qs : path + "?" + qs;
            }

            var response = await httpClient.GetAsync(path, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var products = JsonSerializer.Deserialize<List<Product>>(content, options) ?? new List<Product>();

            return products.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from remote API");
            throw;
        }
    }
}
