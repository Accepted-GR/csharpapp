namespace CSharpApp.Application.Products;

public interface IUpstreamProductMapper
{
    UpstreamCreateProduct Map(CSharpApp.Core.Dtos.Product product);
}
