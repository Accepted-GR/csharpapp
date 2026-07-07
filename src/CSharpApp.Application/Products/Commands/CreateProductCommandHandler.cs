using MediatR;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Settings;

namespace CSharpApp.Application.Products.Commands;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Product?>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<CreateProductCommandHandler> _logger;
    private readonly IUpstreamProductMapper? _upstreamMapper;

    public CreateProductCommandHandler(
        IHttpClientFactory httpClientFactory,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<CreateProductCommandHandler> logger,
        IUpstreamProductMapper? upstreamMapper = null)
    {
        _httpClientFactory = httpClientFactory;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
        _upstreamMapper = upstreamMapper;
    }

    public async Task<Product?> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("RestApiClient");
            var product = request.Product;
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

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

            var response = await httpClient.PostAsync(path, content, cancellationToken);

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
}
