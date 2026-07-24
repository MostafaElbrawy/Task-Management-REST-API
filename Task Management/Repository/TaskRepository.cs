using Microsoft.EntityFrameworkCore;
using Task_Management.Models;

namespace Task_Management.Repository
{
    public class TaskRepository : Repository<Models.Task> , ITaskRepository
    {
        public TaskRepository(ApplicationContext context) : base(context) { }

        public async Task<Models.Task?> GetByIdWithProject(int taskId) 
        {
            var task = await _context.Tasks
                .Where(t => t.Id == taskId)
                .Include(t => t.Project)
                .FirstOrDefaultAsync();
            return task;
        }
    }
}
