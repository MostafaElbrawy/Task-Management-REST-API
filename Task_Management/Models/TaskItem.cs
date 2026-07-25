using System.ComponentModel.DataAnnotations.Schema;
using Task_Management.Enums;

namespace Task_Management.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public required string Title { get; set; } = null!;
        public string? Description { get; set; }
        public Status Status { get; set; } = Status.Todo;
        public Priority Priority { get; set; } = Priority.Medium;
        public DateTime? DueDate { get; set; } = null;
        public required DateTime CreatedAt { get; set; }
        public required DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(Project))]
        public int ProjectId { get; set; }
        public virtual Project Project { get; set; } = null!;
    }

    
}
