using Microsoft.AspNetCore.Identity;

namespace Task_Management.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public virtual ICollection<Project> Projects { get; set; }
         = new HashSet<Project>();
    }
}
