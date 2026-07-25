using Microsoft.EntityFrameworkCore;
using Task_Management.Models;

namespace Task_Management.Tests.TestHelpers
{
    // A minimal DbContext used only to give ITaskRepository/IProjectRepository mocks
    // a real, queryable EF Core data source (needed because TaskService/ProjectService
    // call EF async operators like FirstOrDefaultAsync/CountAsync/ToListAsync on the
    // IQueryable<T> returned by GetAll() — those require a real IAsyncQueryProvider,
    // which a plain Moq-mocked IQueryable cannot provide).
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<Project> Projects => Set<Project>();
        public DbSet<TaskItem> Tasks => Set<TaskItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relax the Project -> ApplicationUser relationship for tests: we only
            // care about the UserId scalar for ownership checks, not a fully seeded
            // Identity user graph.
            modelBuilder.Entity<Project>()
                .HasOne(p => p.User)
                .WithMany(u => u.Projects)
                .HasForeignKey(p => p.UserId)
                .IsRequired(false);
        }
    }

    public static class TestDbContextFactory
    {
        // Every call gets its own isolated in-memory database so tests never
        // bleed state into one another.
        public static TestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new TestDbContext(options);
        }
    }
}
