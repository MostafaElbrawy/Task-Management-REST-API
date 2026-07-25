using FluentValidation;
using Task_Management.DTOs;

namespace Task_Management.Validators
{
    public class CreateUpdateProjectValidator : AbstractValidator<CreateUpdateProjectDto>
    {
        public CreateUpdateProjectValidator() 
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage("Project name is required");
        }
    }
}
