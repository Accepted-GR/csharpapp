namespace CSharpApp.Application.Products;

public interface IProductMapper
{
    CSharpApp.Core.Dtos.Product MapFromCreateRequest(CSharpApp.Core.Dtos.CreateProductRequestDto request);
    CSharpApp.Application.Products.UpstreamCreateProduct MapToUpstream(CSharpApp.Core.Dtos.Product product);
}
