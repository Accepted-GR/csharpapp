namespace CSharpApp.Core.Interfaces;

public interface IProductsService
{
	Task<IReadOnlyCollection<Product>> GetProducts(CancellationToken cancellationToken);

	Task<Product?> GetProductById(int id, CancellationToken cancellationToken);

	Task<int?> CreateProduct(CreateProductPayload payload, CancellationToken cancellationToken);
}