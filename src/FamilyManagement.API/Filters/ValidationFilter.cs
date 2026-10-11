using FamilyManagement.Shared.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FamilyManagement.API.Filters;

/// <summary>Runs the registered FluentValidation validator for every action argument that has one.</summary>
public class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                var errors = result.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
                context.Result = new BadRequestObjectResult(ApiResponse.Fail("Validation failed", errors));
                return;
            }
        }

        await next();
    }
}
