using FluentValidation;
using CSharpApp.Application.Categories.Commands;

namespace CSharpApp.Application.Categories.Commands;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Category)
            .NotNull()
            .WithMessage("Category cannot be null");

        When(x => x.Category != null, () =>
        {
            RuleFor(x => x.Category.Name)
                .NotEmpty()
                .WithMessage("Category name is required")
                .MaximumLength(100)
                .WithMessage("Category name cannot exceed 100 characters");

            RuleFor(x => x.Category.Image)
                .MaximumLength(500)
                .WithMessage("Image URL cannot exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Category.Image));
        });
    }
}
