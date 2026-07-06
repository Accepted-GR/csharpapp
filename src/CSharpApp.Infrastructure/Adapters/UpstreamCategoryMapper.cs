namespace CSharpApp.Infrastructure.Adapters;

using CSharpApp.Application.Categories;

public class UpstreamCategoryMapper : IUpstreamCategoryMapper
{
    public UpstreamCreateCategory Map(CSharpApp.Core.Dtos.Category category)
    {
        return new UpstreamCreateCategory
        {
            Name = category.Name,
            Image = category.Image
        };
    }
}
