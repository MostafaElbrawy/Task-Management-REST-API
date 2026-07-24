namespace Task_Management.DTOs
{
    public class ProjectDto
    {
        public required string Name { get; set; }
        public string? Description { get; set; }
        public required DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
