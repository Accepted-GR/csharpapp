using FluentValidation;
using MediatR;

namespace CSharpApp.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that automatically validates requests using FluentValidation
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // If no validators are registered, skip validation
        if (!_validators.Any())
        {
            return await next();
        }

        // Create validation context
        var context = new ValidationContext<TRequest>(request);

        // Run all validators
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Collect all validation failures
        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        // If there are failures, throw ValidationException
        if (failures.Any())
        {
            throw new ValidationException(failures);
        }

        // Continue to the next behavior or handler
        return await next();
    }
}
