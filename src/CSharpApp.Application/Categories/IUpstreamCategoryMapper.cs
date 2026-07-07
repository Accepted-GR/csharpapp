namespace CSharpApp.Application.Categories;

public interface IUpstreamCategoryMapper
{
    UpstreamCreateCategory Map(CSharpApp.Core.Dtos.Category category);
}
