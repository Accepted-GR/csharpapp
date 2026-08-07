namespace CSharpApp.Core.Interfaces;

public interface IProductsService
{
    Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken = default);

    /// <exception cref="UpstreamNotFoundException">No product has that id.</exception>
    Task<Product> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <exception cref="UpstreamNotFoundException">The referenced category does not exist.</exception>
    Task<Product> CreateProductAsync(ProductWriteModel product, CancellationToken cancellationToken = default);

    /// <summary>Replaces a product in full.</summary>
    /// <remarks>
    /// A full replacement rather than a partial update: the upstream endpoint
    /// answers a partial body with a 500, so every field is required.
    /// </remarks>
    /// <exception cref="UpstreamNotFoundException">No product has that id.</exception>
    Task<Product> UpdateProductAsync(int id, ProductWriteModel product, CancellationToken cancellationToken = default);

    /// <exception cref="UpstreamNotFoundException">No product has that id.</exception>
    Task DeleteProductAsync(int id, CancellationToken cancellationToken = default);
}
