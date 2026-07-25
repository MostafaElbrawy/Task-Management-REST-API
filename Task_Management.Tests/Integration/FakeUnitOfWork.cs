using Microsoft.EntityFrameworkCore;
using Task_Management.Models;
using Task_Management.Repository;
using Task_Management.Tests.TestHelpers;

namespace Task_Management.Tests.Integration
{
    // Minimal real implementations of the repository interfaces, backed by a
    // real (InMemory) DbContext instead of mocks. AddAsync/UpdateAsync/DeleteAsync
    // only stage changes on the context — actual persistence happens in
    // FakeUnitOfWork.CommitAsync(), matching the Unit of Work pattern the real
    // services expect.
    public class FakeRepository<T> : IRepository<T> where T : class
    {
        protected readonly TestDbContext Context;
        public FakeRepository(TestDbContext context) => Context = context;

        public IQueryable<T> GetAll() => Context.Set<T>();

        public async Task<T?> GetByIdAsync(int id) => await Context.Set<T>().FindAsync(id);

        public Task<bool> AddAsync(T entity)
        {
            Context.Set<T>().Add(entity);
            return Task.FromResult(true);
        }

        public Task<bool> UpdateAsync(T entity)
        {
            Context.Set<T>().Update(entity);
            return Task.FromResult(true);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await Context.Set<T>().FindAsync(id);
            if (entity == null) return false;
            Context.Set<T>().Remove(entity);
            return true;
        }
    }

    public class FakeTaskRepository : FakeRepository<TaskItem>, ITaskRepository
    {
        public FakeTaskRepository(TestDbContext context) : base(context) { }

        public async Task<TaskItem?> GetByIdWithProject(int taskId) =>
            await Context.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == taskId);
    }

    public class FakeProjectRepository : FakeRepository<Project>, IProjectRepository
    {
        public FakeProjectRepository(TestDbContext context) : base(context) { }

        public async Task<Project?> GetByNameAsync(string name) =>
            await Context.Projects.FirstOrDefaultAsync(p => p.Name == name);
    }

    public class FakeUnitOfWork : IUnifOfWork
    {
        private readonly TestDbContext _context;
        public FakeUnitOfWork(TestDbContext context)
        {
            _context = context;
            Tasks = new FakeTaskRepository(context);
            Projects = new FakeProjectRepository(context);
        }

        public ITaskRepository Tasks { get; }
        public IProjectRepository Projects { get; }

        public Task<int> CommitAsync() => _context.SaveChangesAsync();
    }
}
