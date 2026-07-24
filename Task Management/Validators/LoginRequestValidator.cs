using FluentValidation;
using Task_Management.DTOs;

namespace Task_Management.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator() 
        {
            RuleFor(l => l.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Email is not valid"); //uniquness is done by user manager

            RuleFor(r => r.Password)
                .NotEmpty().WithMessage("Password is required");
        }
    }
}
