using Microsoft.AspNetCore.Mvc;
using Task_Management.DTOs;
using Task_Management.Enums;
using Task_Management.Models;
using Task_Management.Tests.TestHelpers;
using Xunit;

namespace Task_Management.Tests.Integration
{
    public class CriticalFlowIntegrationTests : IntegrationTestBase
    {
        private const int UserId = 1;

        // ---------- Flow 1: Create project -> add task -> mark done -> delete project ----------

        [Fact]
        public async Task CreateProject_AddTask_MarkDone_DeleteProject_CascadesTaskDeletion()
        {
            AuthenticateAs(ProjectsController, UserId);
            AuthenticateAs(TasksController, UserId);

            // 1. Create project
            var createProjectResult = Assert.IsType<ObjectResult>(
                await ProjectsController.CreateProject(new CreateUpdateProjectDto { Name = "Integration Project" }));
            var createProjectResponse = Assert.IsType<ApiResponse<ProjectDto?>>(createProjectResult.Value);
            Assert.Equal(201, createProjectResponse.StatusCode);
            var projectId = createProjectResponse.Data!.Id;

            // 2. Add task
            var createTaskResult = Assert.IsType<ObjectResult>(
                await TasksController.CreateTask(new CreateTaskDto { Title = "Integration Task" }, projectId));
            var createTaskResponse = Assert.IsType<ApiResponse<TaskDto?>>(createTaskResult.Value);
            Assert.Equal(201, createTaskResponse.StatusCode);
            var taskId = createTaskResponse.Data!.Id;

            // 3. Mark task as done
            var updateResult = Assert.IsType<ObjectResult>(
                await TasksController.UpdateTask(
                    new UpdateTaskDto { Title = "Integration Task", Status = Status.Done, ProjectId = projectId },
                    taskId));
            var updateResponse = Assert.IsType<ApiResponse<TaskDto?>>(updateResult.Value);
            Assert.Equal(200, updateResponse.StatusCode);
            Assert.Equal(Status.Done, updateResponse.Data!.Status);

            // 4. Delete project
            var deleteResult = Assert.IsType<ObjectResult>(await ProjectsController.DeleteProject(projectId));
            var deleteResponse = Assert.IsType<ApiResponse<bool>>(deleteResult.Value);
            Assert.True(deleteResponse.Success);

            // 5. Verify the task was cascade-deleted along with its project
            var remainingTask = await Context.Tasks.FindAsync(taskId);
            Assert.Null(remainingTask);
        }

        // ---------- Flow 2: Filter tasks by status and priority ----------

        [Fact]
        public async Task FilterTasks_ByStatusAndPriority_ReturnsOnlyMatchingTasks()
        {
            AuthenticateAs(TasksController, UserId);

            // Project/TaskItem now use private setters + Create() factories,
            // so entities are built via the domain factory methods rather than
            // object initializers. No Id override needed here since these go
            // through the real context and EF assigns the Id on save.
            var project = Project.Create("Filter Project", null, UserId);
            Context.Projects.Add(project);
            await Context.SaveChangesAsync();

            Context.Tasks.AddRange(
                TaskItem.Create("Match", null, Status.InProgress, Priority.High, null, project.Id),
                TaskItem.Create("Wrong status", null, Status.Todo, Priority.High, null, project.Id),
                TaskItem.Create("Wrong priority", null, Status.InProgress, Priority.Low, null, project.Id)
            );
            await Context.SaveChangesAsync();

            var result = Assert.IsType<ObjectResult>(
                await TasksController.GetProjectTasks(
                    project.Id, Status.InProgress, Priority.High, null, null, null, null, page: 1, pageSize: 10));
            var response = Assert.IsType<ApiResponse<PagedList<TaskDto>>>(result.Value);

            Assert.True(response.Success);
            Assert.Single(response.Data!.Items);
            Assert.Equal("Match", response.Data.Items[0].Title);
        }

        // ---------- Flow 3: Search tasks and verify pagination ----------

        [Fact]
        public async Task SearchTasks_WithPagination_ReturnsCorrectSubsetAndTotalCount()
        {
            AuthenticateAs(TasksController, UserId);

            var project = Project.Create("Search Project", null, UserId);
            Context.Projects.Add(project);
            await Context.SaveChangesAsync();

            // 3 tasks match "payment", 2 don't
            for (int i = 0; i < 3; i++)
            {
                Context.Tasks.Add(TaskItem.Create($"Fix payment issue {i}", null, Status.Todo, Priority.Medium, null, project.Id));
            }
            Context.Tasks.Add(TaskItem.Create("Unrelated work", null, Status.Todo, Priority.Medium, null, project.Id));
            Context.Tasks.Add(TaskItem.Create("Also unrelated", null, Status.Todo, Priority.Medium, null, project.Id));
            await Context.SaveChangesAsync();

            // Page 1 of 2 (pageSize 2) over the 3 "payment" matches
            var result = Assert.IsType<ObjectResult>(
                await TasksController.GetAllTasks("payment", null, null, null, null, null, null, page: 1, pageSize: 2));
            var response = Assert.IsType<ApiResponse<PagedList<TaskDto>>>(result.Value);

            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Items.Count);
            Assert.Equal(3, response.Data.TotalCount);
            Assert.All(response.Data.Items, t => Assert.Contains("payment", t.Title));
        }
    }
}
