using Task_Management.Enums;
namespace Task_Management.DTOs
{
    public class TaskDto
    {
        public required int Id { get; set; }
        public required string ProjectName { get; set; }
        public required string Title { get; set; } = null!;
        public string? Description { get; set; }
        public Status Status { get; set; } = Status.Todo;
        public Priority Priority { get; set; } = Priority.Medium;
        public DateTime? DueDate { get; set; } = null;
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }
    }
}
