using Task_Management.Models;

namespace Task_Management.Repository
{
    public class ProjectRepository : Repository<Project> , IProjectRepository
    {
        public ProjectRepository(ApplicationContext context) : base(context) { }
    }
}
