using FluentValidation;
using Task_Management.DTOs;
using Task_Management.Enums;
namespace Task_Management.Validators
{
    public class CreateTaskValidator : AbstractValidator<CreateTaskDto>
    {
        public CreateTaskValidator()
        {
            RuleFor(c => c.Title)
                .NotEmpty().WithMessage("Title is required");

            RuleFor(c => c.Priority)
                .IsInEnum().Must(p => p != Priority.None).WithMessage("Invalid Priority Value");

            RuleFor(c => c.Status)
                .IsInEnum().Must(s => s != Status.None).WithMessage("Invalid Status Value");

            RuleFor(c => c.DueDate)
                .Must(date => date == null || date >= DateTime.UtcNow).WithMessage("Due date cannot be in the past");
        }
    }
}
