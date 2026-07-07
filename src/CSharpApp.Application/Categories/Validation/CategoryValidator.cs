namespace CSharpApp.Application.Categories.Validation;

public class CategoryValidator : ICategoryValidator
{
    public ValidationResult ValidateForCreate(CSharpApp.Core.Dtos.CreateCategoryRequestDto request)
    {
        var result = new ValidationResult { IsValid = true };

        if (request == null)
        {
            result.IsValid = false;
            result.Errors.Add("Request cannot be null.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.IsValid = false;
            result.Errors.Add("Name is required.");
        }

        return result;
    }
}
