namespace CSharpApp.Api.Validation;

using CSharpApp.Core.Dtos;

public interface ICreateProductValidator
{
    bool Validate(CreateProductRequestDto request, out System.Collections.Generic.List<string> errors);
}

public class CreateProductValidator : ICreateProductValidator
{
    public bool Validate(CreateProductRequestDto request, out System.Collections.Generic.List<string> errors)
    {
        errors = new System.Collections.Generic.List<string>();
        if (request == null)
        {
            errors.Add("Request cannot be null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Title))
            errors.Add("Title is required.");

        if (request.Price.HasValue && request.Price <= 0)
            errors.Add("Price must be greater than zero when provided.");

        if (request.CategoryId.HasValue && request.CategoryId <= 0)
            errors.Add("CategoryId, when provided, must be a positive integer.");

        return errors.Count == 0;
    }
}
