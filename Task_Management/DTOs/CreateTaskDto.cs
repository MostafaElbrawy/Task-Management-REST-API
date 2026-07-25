using Task_Management.Enums;

namespace Task_Management.DTOs
{
    public class CreateTaskDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public Priority? Priority { get; set; }
        public Status? Status { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
