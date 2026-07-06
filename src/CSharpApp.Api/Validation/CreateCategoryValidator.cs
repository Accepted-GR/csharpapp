namespace CSharpApp.Api.Validation;

using CSharpApp.Core.Dtos;

public interface ICreateCategoryValidator
{
    bool Validate(CreateCategoryRequestDto request, out System.Collections.Generic.List<string> errors);
}

public class CreateCategoryValidator : ICreateCategoryValidator
{
    public bool Validate(CreateCategoryRequestDto request, out System.Collections.Generic.List<string> errors)
    {
        errors = new System.Collections.Generic.List<string>();
        if (request == null)
        {
            errors.Add("Request cannot be null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Name is required.");

        return errors.Count == 0;
    }
}
