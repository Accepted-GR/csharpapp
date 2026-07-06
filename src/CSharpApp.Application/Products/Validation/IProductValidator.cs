namespace CSharpApp.Application.Products;

public interface IProductValidator
{
    ValidationResult ValidateForCreate(CSharpApp.Core.Dtos.CreateProductRequestDto request);
}

public sealed class ValidationResult
{
    public bool IsValid { get; set; }
    public System.Collections.Generic.List<string> Errors { get; } = new System.Collections.Generic.List<string>();
}
