using FluentValidation;
using CSharpApp.Application.Products.Commands;

namespace CSharpApp.Application.Products.Commands;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Product)
            .NotNull()
            .WithMessage("Product cannot be null");

        When(x => x.Product != null, () =>
        {
            RuleFor(x => x.Product.Title)
                .NotEmpty()
                .WithMessage("Title is required")
                .MaximumLength(200)
                .WithMessage("Title cannot exceed 200 characters");

            RuleFor(x => x.Product.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than zero")
                .When(x => x.Product.Price.HasValue);

            RuleFor(x => x.Product.Category!.Id)
                .GreaterThan(0)
                .WithMessage("CategoryId must be a positive integer")
                .When(x => x.Product.Category != null && x.Product.Category.Id.HasValue);

            RuleFor(x => x.Product.Description)
                .MaximumLength(1000)
                .WithMessage("Description cannot exceed 1000 characters")
                .When(x => !string.IsNullOrEmpty(x.Product.Description));
        });
    }
}
