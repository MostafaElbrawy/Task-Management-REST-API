using Moq;
using Task_Management.DTOs;
using Task_Management.Models;
using Task_Management.Repository;
using Task_Management.Services;
using Task_Management.Tests.TestHelpers;
using Xunit;

namespace Task_Management.Tests.Services
{
    public class ProjectServiceTests : IDisposable
    {
        private readonly TestDbContext _context;
        private readonly Mock<IProjectRepository> _projectRepo = new();
        private readonly Mock<IUnifOfWork> _uow = new();
        private readonly ProjectService _sut;

        public ProjectServiceTests()
        {
            _context = TestDbContextFactory.Create();
            _projectRepo.Setup(r => r.GetAll()).Returns(() => _context.Projects);
            _uow.Setup(u => u.Projects).Returns(_projectRepo.Object);
            _uow.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var userManager = MockUserManagerFactory.Create();
            _sut = new ProjectService(_uow.Object, userManager.Object);
        }

        public void Dispose() => _context.Dispose();

        private async Task SeedProjects(int userId, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Project.Create() factory replaces the old object-initializer
                // seed; EF assigns the Id on save, so no override needed here.
                _context.Projects.Add(Project.Create($"Project {i}", null, userId));
            }
            await _context.SaveChangesAsync();
        }

        // ---------- PagedProjects ----------

        [Fact]
        public async Task PagedProjects_InvalidPageOrSize_ReturnsValidationError()
        {
            var result = await _sut.PagedProjects(page: 0, pageSize: 10, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task PagedProjects_OnlyReturnsCallingUsersProjects()
        {
            await SeedProjects(userId: 1, count: 3);
            await SeedProjects(userId: 2, count: 2);

            var result = await _sut.PagedProjects(page: 1, pageSize: 10, userId: 1);

            Assert.True(result.Success);
            Assert.Equal(3, result.Data!.TotalCount);
        }

        [Fact]
        public async Task PagedProjects_RespectsPageSize()
        {
            await SeedProjects(userId: 1, count: 5);

            var result = await _sut.PagedProjects(page: 1, pageSize: 2, userId: 1);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Items.Count);
            Assert.Equal(5, result.Data.TotalCount);
        }

        // ---------- Project (get one) ----------

        [Fact]
        public async Task Project_NotFound_ReturnsNotFound()
        {
            _projectRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Project?)null);

            var result = await _sut.GetProject(1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task Project_BelongsToDifferentUser_ReturnsForbid()
        {
            var project = TestEntityFactory.Project(id: 1, name: "P", userId: 2);
            _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

            var result = await _sut.GetProject(1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task Project_OwnedByUser_ReturnsOk()
        {
            var project = TestEntityFactory.Project(id: 1, name: "P", userId: 1);
            _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

            var result = await _sut.GetProject(1, userId: 1);

            Assert.True(result.Success);
            Assert.Equal("P", result.Data!.Name);
        }

        // ---------- Create ----------

        [Fact]
        public async Task Create_DuplicateName_ReturnsValidationError()
        {
            _projectRepo.Setup(r => r.GetByNameAsync("Existing"))
                .ReturnsAsync(TestEntityFactory.Project(id: 1, name: "Existing", userId: 1));

            var dto = new CreateUpdateProjectDto { Name = "Existing" };
            var result = await _sut.Create(dto, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task Create_UniqueName_ReturnsCreated()
        {
            _projectRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((Project?)null);
            _projectRepo.Setup(r => r.AddAsync(It.IsAny<Project>())).ReturnsAsync(true);

            var dto = new CreateUpdateProjectDto { Name = "New Project", Description = "Desc" };
            var result = await _sut.Create(dto, userId: 1);

            Assert.True(result.Success);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal("New Project", result.Data!.Name);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task Create_RepositoryAddFails_ReturnsFail()
        {
            _projectRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((Project?)null);
            _projectRepo.Setup(r => r.AddAsync(It.IsAny<Project>())).ReturnsAsync(false);

            var dto = new CreateUpdateProjectDto { Name = "New Project" };
            var result = await _sut.Create(dto, userId: 1);

            Assert.False(result.Success);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        // ---------- Update ----------

        [Fact]
        public async Task Update_ProjectNotFound_ReturnsNotFound()
        {
            _projectRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Project?)null);

            var dto = new CreateUpdateProjectDto { Name = "Whatever" };
            var result = await _sut.Update(dto, projectId: 1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task Update_BelongsToDifferentUser_ReturnsForbid()
        {
            var project = TestEntityFactory.Project(id: 1, name: "P", userId: 2);
            _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

            var dto = new CreateUpdateProjectDto { Name = "Whatever" };
            var result = await _sut.Update(dto, projectId: 1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task Update_RenamingToExistingName_ReturnsValidationError()
        {
            var project = TestEntityFactory.Project(id: 1, name: "Old Name", userId: 1);
            _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
            _projectRepo.Setup(r => r.GetByNameAsync("Taken Name"))
                .ReturnsAsync(TestEntityFactory.Project(id: 2, name: "Taken Name", userId: 1));

            var dto = new CreateUpdateProjectDto { Name = "Taken Name" };
            var result = await _sut.Update(dto, projectId: 1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task Update_KeepingSameName_DoesNotTriggerDuplicateCheck()
        {
            var project = TestEntityFactory.Project(id: 1, name: "Same Name", userId: 1);
            _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
            _projectRepo.Setup(r => r.UpdateAsync(It.IsAny<Project>())).ReturnsAsync(true);

            var dto = new CreateUpdateProjectDto { Name = "Same Name", Description = "Updated description" };
            var result = await _sut.Update(dto, projectId: 1, userId: 1);

            Assert.True(result.Success);
            _projectRepo.Verify(r => r.GetByNameAsync(It.IsAny<string>()), Times.Never);
        }

        // ---------- Delete ----------

        [Fact]
        public async Task Delete_NotFound_ReturnsNotFound()
        {
            _projectRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Project?)null);

            var result = await _sut.Delete(1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(404, result.StatusCode);
        }

        [Fact]
        public async Task Delete_BelongsToDifferentUser_ReturnsForbid()
        {
            var project = TestEntityFactory.Project(id: 1, name: "P", userId: 2);
            _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);

            var result = await _sut.Delete(1, userId: 1);

            Assert.False(result.Success);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task Delete_Valid_ReturnsOkAndCommits()
        {
            var project = TestEntityFactory.Project(id: 1, name: "P", userId: 1);
            _projectRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(project);
            _projectRepo.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            var result = await _sut.Delete(1, userId: 1);

            Assert.True(result.Success);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}
