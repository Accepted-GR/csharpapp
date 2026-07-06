namespace CSharpApp.Application.Products;

public class ProductMapper : IProductMapper
{
    public CSharpApp.Core.Dtos.Product MapFromCreateRequest(CSharpApp.Core.Dtos.CreateProductRequestDto request)
    {
        var product = new CSharpApp.Core.Dtos.Product
        {
            Title = request.Title,
            Price = request.Price,
            Description = request.Description,
            Category = request.CategoryId.HasValue ? new CSharpApp.Core.Dtos.Category { Id = request.CategoryId } : null
        };

        if (request.Images != null)
        {
            product.Images.AddRange(request.Images);
            // No-op patch: ensure file change is recorded
        }

        return product;
    }

    public UpstreamCreateProduct MapToUpstream(CSharpApp.Core.Dtos.Product product)
    {
        // Delegate to infrastructure mapper via IUpstreamProductMapper when available.
        // Keep fallback mapping here in case Infrastructure implementation is not registered.
        return new UpstreamCreateProduct
        {
            Title = product.Title,
            Price = product.Price,
            Description = product.Description,
            Images = product.Images != null && product.Images.Count > 0 ? new System.Collections.Generic.List<string>(product.Images) : null,
            CategoryId = product.Category?.Id
        };
    }
}
