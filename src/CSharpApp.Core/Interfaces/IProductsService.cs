namespace CSharpApp.Core.Interfaces;

public interface IProductsService
{
    Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken = default);
}
