using System.ComponentModel.DataAnnotations.Schema;
using Task_Management.Enums;

namespace Task_Management.Models
{
    public class TaskItem
    {
        public int Id { get; private set; }
        public string Title { get; private set; } = null!;
        public string? Description { get; private set; }
        public Status Status { get; private set; } = Status.Todo;
        public Priority Priority { get; private set; } = Priority.Medium;
        public DateTime? DueDate { get; private set; } = null;
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        [ForeignKey(nameof(Project))]
        public int ProjectId { get; private set; }
        public virtual Project Project { get; private set; } = null!;

        private TaskItem() { }

        private TaskItem(string title, string? description, Status status,
            Priority priority, DateTime? dueDate, DateTime createdAt,DateTime updatedAt, int projectId)
        {
            Title = title;
            Description = description;
            Status = status;
            Priority = priority;
            DueDate = dueDate;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            ProjectId = projectId;
        }
        public static TaskItem Create(string title , string? description, Status status,
            Priority priority , DateTime? dueDate , int projectId)
        {
            return new TaskItem(title, description, status, 
                priority, dueDate, DateTime.UtcNow, DateTime.UtcNow, projectId);
        }

        public void Update(string title, string? description, Status status,
            Priority priority, DateTime? dueDate, int projectId)
        {
            Title = title;
            Description = description;
            Status = status;
            Priority= priority;
            DueDate = dueDate;
            ProjectId= projectId;
            UpdatedAt= DateTime.UtcNow;
        }
    }

    
}
