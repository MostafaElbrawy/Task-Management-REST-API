using FluentValidation;
using Task_Management.Enums;
using Task_Management.DTOs;
namespace Task_Management.Validators
{
    public class UpdateTaskValidator : AbstractValidator<UpdateTaskDto>
    {
        public UpdateTaskValidator() 
        {
            RuleFor(c => c.Title)
                .NotEmpty().WithMessage("Title is required");

            RuleFor(c => c.Priority)
                .IsInEnum().Must(p => p != Priority.None).WithMessage("Invalid Priority Value");
                
            RuleFor(c => c.Status)
                .IsInEnum().Must(s => s != Status.None).WithMessage("Invalid Status Value");


        }
    }
}
