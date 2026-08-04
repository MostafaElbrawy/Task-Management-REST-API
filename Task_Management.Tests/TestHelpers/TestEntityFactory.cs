using System.Reflection;
using Task_Management.Enums;
using Task_Management.Models;

namespace Task_Management.Tests.TestHelpers
{
    // Project/TaskItem now encapsulate their state behind private setters and
    // only expose Create()/Update() — no more `new Project { Id = 1, ... }`.
    // For tests that hand a fully-formed entity to a mocked repository (rather
    // than seeding through a real DbContext, which auto-assigns Id), we still
    // need to pin a specific Id. Reflection is the only way to do that from
    // outside the assembly, so it's confined to this one file instead of
    // scattered across every test.
    public static class TestEntityFactory
    {
        public static Project Project(int id, string name, int userId, string? description = null)
        {
            var project = Models.Project.Create(name, description, userId);
            SetProperty(project, nameof(Models.Project.Id), id);
            return project;
        }

        public static TaskItem Task(
            int id, string title, int projectId,
            Status status = Status.Todo, Priority priority = Priority.Medium,
            Project? project = null, string? description = null, DateTime? dueDate = null)
        {
            var task = Models.TaskItem.Create(title, description, status, priority, dueDate, projectId);
            SetProperty(task, nameof(Models.TaskItem.Id), id);
            if (project != null)
            {
                SetProperty(task, nameof(Models.TaskItem.Project), project);
            }
            return task;
        }

        // General-purpose escape hatch for overriding any private-setter
        // property (e.g. CreatedAt/UpdatedAt) that Create()/Update() don't
        // expose as parameters.
        public static void SetProperty<T>(T entity, string propertyName, object value)
        {
            var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"{typeof(T).Name} has no property named '{propertyName}'.");
            prop.SetValue(entity, value);
        }
    }
}
