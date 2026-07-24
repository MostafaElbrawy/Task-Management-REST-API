using Microsoft.EntityFrameworkCore;
using Task_Management.Models;

namespace Task_Management.Repository
{
    public class ProjectRepository : Repository<Project> , IProjectRepository
    {
        public ProjectRepository(ApplicationContext context) : base(context) { }

        public async Task<Project?> GetByNameAsync(string name)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Name == name);
            return project;
        }
    }
}
