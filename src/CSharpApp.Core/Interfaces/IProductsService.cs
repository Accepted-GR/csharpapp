namespace CSharpApp.Core.Interfaces;

public interface IProductsService
{
    Task<IReadOnlyCollection<Product>> GetProducts(int? offset = null, int? limit = null);
    Task<Product?> GetProductById(int id, CancellationToken cancellationToken = default);
    Task<Product?> CreateProduct(Product product, CancellationToken cancellationToken = default);
}