using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using CSharpApp.Core.Dtos;
using CSharpApp.Core.Settings;

namespace CSharpApp.Application.Categories.Commands;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Category?>
{
    private readonly HttpClient _httpClient;
    private readonly RestApiSettings _restApiSettings;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    public CreateCategoryCommandHandler(
        HttpClient httpClient,
        IOptions<RestApiSettings> restApiSettings,
        ILogger<CreateCategoryCommandHandler> logger)
    {
        _httpClient = httpClient;
        _restApiSettings = restApiSettings.Value;
        _logger = logger;
    }

    public async Task<Category?> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var category = request.Category;

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
