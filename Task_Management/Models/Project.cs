using System.ComponentModel.DataAnnotations.Schema;

namespace Task_Management.Models
{
    public class Project
    {
        public int Id { get; private set; }
        public string Name { get; private set; } = null!; 
        public string? Description { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; private set; }
        public virtual ApplicationUser User { get; private set; } = null!;

        public virtual ICollection<TaskItem> Tasks { get; private set; }
         = new HashSet<TaskItem>();

        private Project(string name , string? description, 
            DateTime createdAt , DateTime updatedAt,int userId) 
        {
            Name = name;
            Description = description;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            UserId = userId;

        }
        
        private Project() { }

        public static Project Create(string name, string? description , int userId)
        {
            return new Project(name, description, DateTime.UtcNow, DateTime.UtcNow ,userId);
        }

        public void Update(string name, string? description, int userId)
        {
            Name =name;
            Description = description;
            UpdatedAt=DateTime.UtcNow;
        }
    }
}
