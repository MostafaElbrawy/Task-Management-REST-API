using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Task_Management.DTOs;
using Task_Management.Enums;
using Task_Management.Models;
using Task_Management.Repository;
using Task_Management.Services;
using Task_Management.Tests.TestHelpers;
using Xunit;

namespace Task_Management.Tests.Services
{
    public class TaskServiceTests : IDisposable
    {
        private readonly TestDbContext _context;
        private readonly Mock<ITaskRepository> _taskRepo = new();
        private readonly Mock<IProjectRepository> _projectRepo = new();
        private readonly Mock<IUnifOfWork> _uow = new();
        private readonly Mock<ILogger<TaskService>> _logger = new(); 
        private readonly TaskService _sut;

        public TaskServiceTests()
        {
            _context = TestDbContextFactory.Create();

            // GetAll() returns a real, queryable EF Core source backed by the
            // in-memory database, Include'd so t.Project is always populated.
            _taskRepo.Setup(r => r.GetAll()).Returns(() => _context.Tasks.Include(t => t.Project));

            _uow.Setup(u => u.Tasks).Returns(_taskRepo.Object);
            _uow.Setup(u => u.Projects).Returns(_projectRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var userManager = MockUserManagerFactory.Create();
            _sut = new TaskService(_uow.Object, userManager.Object,_logger.Object);
        }

        public void Dispose() => _context.Dispose();

        // ---------- helpers ----------

        private async Task<Project> SeedProject(int userId, string name = "Project A")
        {
            var project = new Project
            {
                Name = name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = userId
            };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // GetProjectTasks resolves the project via Projects.GetByIdAsync
            // directly (not by inferring it from the tasks query), so the mock
            // needs to know about it too.
            _projectRepo.Setup(r => r.GetByIdAsync(project.Id)).ReturnsAsync(project);

            return project;
        }

        private async Task<TaskItem> SeedTask(
            int projectId, string title, Status status, Priority priority,
            DateTime? dueDate = null, DateTime? createdAt = null)
        {
            var created = createdAt ?? DateTime.UtcNow;
            var task = new TaskItem
            {
                Title = title,
                Status = status,
                Priority = priority,
                DueDate = dueDate,
                CreatedAt = created,
                UpdatedAt = created,
                ProjectId = projectId
            };
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        // ---------- GetProjectTasks: enum guards ----------

        [Fact]
        public async Task GetProjectTasks_StatusNone_ReturnsValidationError()
        {
            var result = await _sut.GetProjectTasks(1, 1, Status.None, null, null, null, null, null, 1, 10);

            Assert.False(result.Success);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task GetProjectTasks_PriorityNone_ReturnsValidationError()
        {
            var result = await _sut.GetProjectTasks(1, 1, null, Priority.None, null, null, null, null, 1, 10);

            Assert.False(result.Success);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task GetProjectTasks_SortColumnNone_ReturnsValidationError()
        {
            var result = await _sut.GetProjectTasks(1, 1, null, null, null, null, TaskSortColumn.None, null, 1, 10);

            Assert.False(result.Success);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task GetProjectTasks_SortOptionNone_ReturnsValidationError()
        {
            var result = await _sut.GetProjectTasks(1, 1, null, null, null, null, null, SortOption.None, 1, 10);

            Assert.False(result.Success);
            Assert.Equal(422, result.StatusCode);
        }

        // NOTE: this test documents the INTENDED behavior. As of the last code
        // review, GetProjectTasks is missing a `return` before the NotFound call
        // and infers project existence from the tasks query rather than checking
        // the project directly — so this will currently fail until both are fixed.
        [Fact]
        public async Task GetProjectTasks_ProjectDoesNotExist_ReturnsNotFound()
        {
            var result = await _sut.GetProjectTasks(999, 1, null, null, null, null, null, null, 1, 10);

            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetProjectTasks_FiltersByStatusAndPriority()
        {
            var project = await SeedProject(userId: 1);
            await SeedTask(project.Id, "Match", Status.Todo, Priority.High);
            await SeedTask(project.Id, "Wrong status", Status.Done, Priority.High);
            await SeedTask(project.Id, "Wrong priority", Status.Todo, Priority.Low);

            var result = await _sut.GetProjectTasks(project.Id, 1, Status.Todo, Priority.High, null, null, null, null, 1, 10);

            Assert.True(result.Success);
            Assert.Single(result.Data!.Items);
            Assert.Equal("Match", result.Data.Items[0].Title);
        }

        [Fact]
        public async Task GetProjectTasks_FiltersByDueDateRange()
        {
            var project = await SeedProject(userId: 1);
            var today = DateTime.UtcNow.Date;
            await SeedTask(project.Id, "In range", Status.Todo, Priority.Medium, dueDate: today.AddDays(2));
            await SeedTask(project.Id, "Too early", Status.Todo, Priority.Medium, dueDate: today.AddDays(-5));
            await SeedTask(project.Id, "Too late", Status.Todo, Priority.Medium, dueDate: today.AddDays(30));

            var result = await _sut.GetProjectTasks(
                project.Id, 1, null, null, today, today.AddDays(7), null, null, 1, 10);

            Assert.True(result.Success);
            Assert.Single(result.Data!.Items);
            Assert.Equal("In range", result.Data.Items[0].Title);
        }

        [Fact]
        public async Task GetProjectTasks_SortsByDueDateDescending()
        {
            var project = await SeedProject(userId: 1);
            var today = DateTime.UtcNow.Date;
            await SeedTask(project.Id, "Earliest", Status.Todo, Priority.Medium, dueDate: today.AddDays(1));
            await SeedTask(project.Id, "Latest", Status.Todo, Priority.Medium, dueDate: today.AddDays(10));
            await SeedTask(project.Id, "Middle", Status.Todo, Priority.Medium, dueDate: today.AddDays(5));

            var result = await _sut.GetProjectTasks(
                project.Id, 1, null, null, null, null, TaskSortColumn.DueDate, SortOption.Desc, 1, 10);

            Assert.True(result.Success);
            var titles = result.Data!.Items.Select(t => t.Title).ToList();
            Assert.Equal(new[] { "Latest", "Middle", "Earliest" }, titles);
        }

        [Fact]
        public async Task GetProjectTasks_Pagination_ReturnsCorrectPageAndTotalCount()
        {
            var project = await SeedProject(userId: 1);
            for (int i = 0; i < 5; i++)
                await SeedTask(project.Id, $"Task {i}", Status.Todo, Priority.Medium);

            var result = await _sut.GetProjectTasks(project.Id, 1, null, null, null, null, null, null, page: 2, pageSize: 2);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Items.Count);
            Assert.Equal(5, result.Data.TotalCount);
            Assert.Equal(2, result.Data.Page);
        }

        // ---------- GetAllTasks ----------

        [Fact]
        public async Task GetAllTasks_OnlyReturnsCallingUsersTasks()
        {
            var myProject = await SeedProject(userId: 1, name: "Mine");
            var otherProject = await SeedProject(userId: 2, name: "Not mine");
            await SeedTask(myProject.Id, "My task", Status.Todo, Priority.Medium);
            await SeedTask(otherProject.Id, "Their task", Status.Todo, Priority.Medium);

            var result = await _sut.GetAllTasks(1, null, null, null, null, null, null, null, 1, 10);

            Assert.True(result.Success);
            Assert.Single(result.Data!.Items);
            Assert.Equal("My task", result.Data.Items[0].Title);
        }

        [Fact]
        public async Task GetAllTasks_SearchTerm_MatchesTitleOrDescription()
        {
            var project = await SeedProject(userId: 1);
            await SeedTask(project.Id, "Fix payment gateway", Status.Todo, Priority.Medium);
            await SeedTask(project.Id, "Unrelated work", Status.Todo, Priority.Medium);

            var result = await _sut.GetAllTasks(1, "payment", null, null, null, null, null, null, 1, 10);

            Assert.True(result.Success);
            Assert.Single(result.Data!.Items);
            Assert.Equal("Fix payment gateway", result.Data.Items[0].Title);
        }

        [Fact]
        public async Task GetAllTasks_StatusNone_ReturnsValidationError()
        {
            var result = await _sut.GetAllTasks(1, null, Status.None, null, null, null, null, null, 1, 10);

            Assert.False(result.Success);
            Assert.Equal(422, result.StatusCode);
        }

        // ---------- GetTask ----------

        [Fact]
        public async Task GetTask_NotFound_ReturnsNotFound()
        {
            _taskRepo.Setup(r => r.GetByIdWithProject(It.IsAny<int>())).ReturnsAsync((TaskItem?)null);

            var result = await _sut.GetTask(1, 1);

            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task GetTask_BelongsToDifferentUser_ReturnsForbid()
        {
            var project = new Project { Id = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, UserId = 2 };
            var task = new TaskItem { Id = 1, Title = "T", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, ProjectId = 1, Project = project };
            _taskRepo.Setup(r => r.GetByIdWithProject(1)).ReturnsAsync(task);

            var result = await _sut.GetTask(1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task GetTask_OwnedByUser_ReturnsOk()
        {
            var project = new Project { Id = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, UserId = 1 };
            var task = new TaskItem { Id = 1, Title = "T", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, ProjectId = 1, Project = project };
            _taskRepo.Setup(r => r.GetByIdWithProject(1)).ReturnsAsync(task);

            var result = await _sut.GetTask(1, userId: 1);

            Assert.True(result.Success);
            Assert.Equal("T", result.Data!.Title);
        }

        // ---------- Create ----------

        [Fact]
        public async Task Create_ProjectNotFound_ReturnsNotFound()
        {
            _projectRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Project?)null);

            var dto = new CreateTaskDto { Title = "New task" };
            var result = await _sut.Create(dto, projectId: 1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task Create_ProjectBelongsToDifferentUser_ReturnsForbid()
        {
            var project = new Project { Id = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, UserId = 2 };
            _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

            var dto = new CreateTaskDto { Title = "New task" };
            var result = await _sut.Create(dto, projectId: 1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task Create_NoStatusOrPriorityProvided_AppliesDefaults()
        {
            var project = new Project { Id = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, UserId = 1 };
            _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
            _taskRepo.Setup(r => r.AddAsync(It.IsAny<TaskItem>())).ReturnsAsync(true);

            var dto = new CreateTaskDto { Title = "New task" };
            var result = await _sut.Create(dto, projectId: 1, userId: 1);

            Assert.True(result.Success);
            Assert.Equal(Status.Todo, result.Data!.Status);
            Assert.Equal(Priority.Medium, result.Data.Priority);
        }

        [Fact]
        public async Task Create_RepositoryAddFails_ReturnsFail()
        {
            var project = new Project { Id = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, UserId = 1 };
            _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
            _taskRepo.Setup(r => r.AddAsync(It.IsAny<TaskItem>())).ReturnsAsync(false);

            var dto = new CreateTaskDto { Title = "New task" };
            var result = await _sut.Create(dto, projectId: 1, userId: 1);

            Assert.False(result.Success);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        // ---------- Update ----------

        [Fact]
        public async Task Update_TaskNotFound_ReturnsNotFound()
        {
            _taskRepo.Setup(r => r.GetByIdWithProject(It.IsAny<int>())).ReturnsAsync((TaskItem?)null);

            var dto = new UpdateTaskDto { Title = "Updated", ProjectId = 1 };
            var result = await _sut.Update(dto, taskId: 1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task Update_TaskBelongsToDifferentUser_ReturnsForbid()
        {
            var project = new Project { Id = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, UserId = 2 };
            var task = new TaskItem { Id = 1, Title = "T", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, ProjectId = 1, Project = project };
            _taskRepo.Setup(r => r.GetByIdWithProject(1)).ReturnsAsync(task);

            var dto = new UpdateTaskDto { Title = "Updated", ProjectId = 1 };
            var result = await _sut.Update(dto, taskId: 1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task Update_TargetProjectNotFound_ReturnsNotFound()
        {
            var currentProject = new Project { Id = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, UserId = 1 };
            var task = new TaskItem { Id = 1, Title = "T", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, ProjectId = 1, Project = currentProject };
            _taskRepo.Setup(r => r.GetByIdWithProject(1)).ReturnsAsync(task);
            _projectRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Project?)null);

            var dto = new UpdateTaskDto { Title = "Updated", ProjectId = 999 };
            var result = await _sut.Update(dto, taskId: 1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task Update_TargetProjectBelongsToDifferentUser_ReturnsForbid()
        {
            var currentProject = new Project { Id = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, UserId = 1 };
            var otherProject = new Project { Id = 2, Name = "Other", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, UserId = 2 };
            var task = new TaskItem { Id = 1, Title = "T", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, ProjectId = 1, Project = currentProject };
            _taskRepo.Setup(r => r.GetByIdWithProject(1)).ReturnsAsync(task);
            _projectRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(otherProject);

            var dto = new UpdateTaskDto { Title = "Updated", ProjectId = 2 };
            var result = await _sut.Update(dto, taskId: 1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task Update_ValidRequest_UpdatesFieldsAndReturnsOk()
        {
            var project = new Project { Id = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, UserId = 1 };
            var task = new TaskItem { Id = 1, Title = "Old title", Status = Status.Todo, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, ProjectId = 1, Project = project };
            _taskRepo.Setup(r => r.GetByIdWithProject(1)).ReturnsAsync(task);
            _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
            _taskRepo.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>())).ReturnsAsync(true);

            var dto = new UpdateTaskDto { Title = "New title", Status = Status.InProgress, Priority = Priority.High, ProjectId = 1 };
            var result = await _sut.Update(dto, taskId: 1, userId: 1);

            Assert.True(result.Success);
            Assert.Equal("New title", result.Data!.Title);
            Assert.Equal(Status.InProgress, result.Data.Status);
        }

        // ---------- Delete ----------

        [Fact]
        public async Task Delete_TaskNotFound_ReturnsNotFound()
        {
            _taskRepo.Setup(r => r.GetByIdWithProject(It.IsAny<int>())).ReturnsAsync((TaskItem?)null);

            var result = await _sut.Delete(1, 1);

            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task Delete_BelongsToDifferentUser_ReturnsForbid()
        {
            var project = new Project { Id = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, UserId = 2 };
            var task = new TaskItem { Id = 1, Title = "T", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, ProjectId = 1, Project = project };
            _taskRepo.Setup(r => r.GetByIdWithProject(1)).ReturnsAsync(task);

            var result = await _sut.Delete(1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task Delete_Valid_ReturnsOkAndCommits()
        {
            var project = new Project { Id = 1, Name = "P", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, UserId = 1 };
            var task = new TaskItem { Id = 1, Title = "T", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, ProjectId = 1, Project = project };
            _taskRepo.Setup(r => r.GetByIdWithProject(1)).ReturnsAsync(task);
            _taskRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            var result = await _sut.Delete(1, userId: 1);

            Assert.True(result.Success);
            Assert.True(result.Data);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}
