namespace CSharpApp.Core.Interfaces;

public interface IProductsService
{
    Task<IReadOnlyCollection<Product>> GetProducts();
    Task<Product?> GetProductById(int id, CancellationToken cancellationToken = default);
    Task<Product?> CreateProduct(Product product, CancellationToken cancellationToken = default);
}