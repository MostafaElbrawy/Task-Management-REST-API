using Task_Management.Models;

namespace Task_Management.Repository
{
    public interface IProjectRepository : IRepository<Project>
    {
        Task<Project?> GetByNameAsync(string name);
    }
}
