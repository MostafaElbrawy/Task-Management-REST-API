using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Task_Management.Models
{
    public class Project
    {
        public int Id { get; set; }
        public required string Name { get; set; } = null!; 
        public string? Description { get; set; }
        public required DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(User))]
        public required int UserId { get; set; }
        public virtual ApplicationUser User { get; set; } = null!;

        public virtual ICollection<Task> Tasks { get; set; }
         = new HashSet<Task>();
    }
}
