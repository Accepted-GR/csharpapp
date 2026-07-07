namespace CSharpApp.Application.Categories.Mapping;

public interface ICategoryMapper
{
    CSharpApp.Core.Dtos.Category MapFromCreateRequest(CSharpApp.Core.Dtos.CreateCategoryRequestDto request);
}
