namespace CSharpApp.Application.Categories;

public interface ICategoryValidator
{
    ValidationResult ValidateForCreate(CSharpApp.Core.Dtos.CreateCategoryRequestDto request);
}

public sealed class ValidationResult
{
    public bool IsValid { get; set; }
    public System.Collections.Generic.List<string> Errors { get; } = new System.Collections.Generic.List<string>();
}
