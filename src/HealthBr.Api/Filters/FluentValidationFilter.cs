using FluentValidation;
using FluentValidation.Results;

using Microsoft.AspNetCore.Mvc.Filters;

namespace HealthBr.Api.Filters;

/// <summary>
/// Runs every registered FluentValidation validator that matches an action
/// argument before the action executes, translating failures into a
/// <see cref="ValidationException"/> handled by the global error pipeline as
/// <c>400 application/problem+json</c> with per-field <c>details</c> (spec
/// 10.1/15.2). Controllers stay free of manual validation.
/// </summary>
public sealed class FluentValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var failures = new List<ValidationFailure>();
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument),
                context.HttpContext.RequestAborted);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        await next();
    }
}
