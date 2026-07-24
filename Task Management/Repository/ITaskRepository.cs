namespace Task_Management.Repository
{
    public interface ITaskRepository : IRepository<Models.Task>
    {
        Task<Models.Task?> GetByIdWithProject(int taskId);
    }
}
