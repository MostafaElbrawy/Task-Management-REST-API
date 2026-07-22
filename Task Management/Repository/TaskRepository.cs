using Task_Management.Models;

namespace Task_Management.Repository
{
    public class TaskRepository : Repository<Models.Task> , ITaskRepository
    {
        public TaskRepository(ApplicationContext context) : base(context) { }
    }
}
