namespace CSharpApp.Api.Endpoints;

/// <summary>
/// Runs the registered FluentValidation validator for a request body and short
/// circuits with a 400 if it fails.
/// </summary>
/// <remarks>
/// An endpoint filter rather than a MediatR pipeline behaviour, because this
/// solution deliberately keeps plain services rather than adopting CQRS. It gives
/// the same guarantee at the boundary: an endpoint can assume its body is valid,
/// and no endpoint can forget to check.
/// </remarks>
public sealed class ValidationFilter<T> : IEndpointFilter
    where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();

        if (argument is null)
        {
            // A missing or unparseable body. Model binding already rejects the
            // malformed case, so this only guards against a literal "null" body.
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = ["A request body is required."],
            });
        }

        var result = await _validator.ValidateAsync(argument, context.HttpContext.RequestAborted);

        if (!result.IsValid)
        {
            // Every rule runs, so the caller sees all problems at once rather than
            // discovering them one request at a time.
            return TypedResults.ValidationProblem(result.ToDictionary());
        }

        return await next(context);
    }
}
