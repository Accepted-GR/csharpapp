namespace CSharpApp.Application.Products;

public class ProductValidator : IProductValidator
{
    public ValidationResult ValidateForCreate(CSharpApp.Core.Dtos.CreateProductRequestDto request)
    {
        var result = new ValidationResult { IsValid = true };

        if (request == null)
        {
            result.IsValid = false;
            result.Errors.Add("Request cannot be null.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            result.IsValid = false;
            result.Errors.Add("Title is required.");
        }

        if (request.Price.HasValue && request.Price <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("Price must be greater than zero when provided.");
        }

        if (request.CategoryId.HasValue && request.CategoryId <= 0)
        {
            result.IsValid = false;
            result.Errors.Add("CategoryId, when provided, must be a positive integer.");
        }

        return result;
    }
}
