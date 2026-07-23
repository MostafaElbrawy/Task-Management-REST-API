using Azure;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Task_Management.DTOs;

namespace Task_Management.Filters
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context) { }

        private readonly IServiceProvider _serviceProvider;

        public ValidationFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {

            // Find any action arguments that might have a validator
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null) continue;
                
                // Look for a registered validator for this argument's type
                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                var validator = _serviceProvider.GetService(validatorType) as IValidator;

                if (validator != null)
                {
                    // Execute validation asynchronously
                    var contextInstance = new ValidationContext<object>(argument);
                    var validationResult = await validator.ValidateAsync(contextInstance);

                    if (!validationResult.IsValid)
                    {
                        //Extract errors into your custom ApiResponse wrapper
                        var messages = validationResult.Errors
                            .Select(error => error.ErrorMessage)
                            .ToList();

                        var response = ApiResponse<bool>.ValidationError(messages);

                        context.Result = new ObjectResult(response)
                        {
                            StatusCode = response.StatusCode
                        };

                        return; // Short-circuit the request pipeline
                    }
                }
            }

            await next(); // Continue to the controller if valid
        }

         
    }
}
