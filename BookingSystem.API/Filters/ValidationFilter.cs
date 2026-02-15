using BookingSystem.Application.DTOs.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BookingSystem.API.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (_, value) in context.ActionArguments)
        {
            if (value == null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(value.GetType());
            var validator = _serviceProvider.GetService(validatorType) as IValidator;
            if (validator == null) continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(value));
            if (result.IsValid) continue;

            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            context.Result = new BadRequestObjectResult(
                ApiResponse<object>.FailureResponse("One or more validation errors occurred.", errors));
            return;
        }

        await next();
    }
}
