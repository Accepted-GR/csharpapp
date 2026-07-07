namespace CSharpApp.Application.Categories.Mapping;

public class CategoryMapper : ICategoryMapper
{
    public CSharpApp.Core.Dtos.Category MapFromCreateRequest(CSharpApp.Core.Dtos.CreateCategoryRequestDto request)
    {
        return new CSharpApp.Core.Dtos.Category
        {
            Name = request.Name,
            Image = request.Image
        };
    }
}
