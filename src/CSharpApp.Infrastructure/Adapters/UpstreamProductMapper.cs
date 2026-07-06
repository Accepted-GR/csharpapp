namespace CSharpApp.Infrastructure.Adapters;

using CSharpApp.Application.Products;

public class UpstreamProductMapper : IUpstreamProductMapper
{
    public UpstreamCreateProduct Map(CSharpApp.Core.Dtos.Product product)
    {
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
