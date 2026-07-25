using Task_Management.Models;

namespace Task_Management.Repository
{
    public class UnitOfWork : IUnifOfWork
    {
        private readonly ApplicationContext _context;
        public ITaskRepository Tasks { get; private set; }
        public IProjectRepository Projects { get; private set; }

        public UnitOfWork(ApplicationContext context,
            ITaskRepository taskRepository , IProjectRepository projectRepository)
        {
            _context = context;
            Tasks = taskRepository;
            Projects = projectRepository;
        }

        public async Task<int> CommitAsync() => await _context.SaveChangesAsync();

    }
}
