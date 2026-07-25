using Task_Management.Models;
namespace Task_Management.Repository
{
    public interface ITaskRepository : IRepository<TaskItem>
    {
        Task<TaskItem?> GetByIdWithProject(int taskId);
    }
}
