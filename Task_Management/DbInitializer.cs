using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Task_Management.Enums;
using Task_Management.Models;

namespace Task_Management.Data;

public static class DbInitializer
{
    // Same password for both demo users — meets ASP.NET Core Identity's
    // default password policy (upper, lower, digit, non-alphanumeric, 6+ chars).
    private const string DemoPassword = "Password123!";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.MigrateAsync();

        if (await context.Set<Project>().AnyAsync())
        {
            return;
        }

        var alice = await CreateUserAsync(userManager, "alice.johnson@example.com");
        var bob = await CreateUserAsync(userManager, "bob.smith@example.com");

        var now = DateTime.UtcNow;

        var projectNames = new (string Name, string? Description)[]
        {
            // Alice's 4 projects
            ("E-Commerce Platform Redesign", "Rebuilding the storefront and checkout flow."),
            ("Mobile App Backend", "REST API powering the iOS/Android clients."),
            ("Marketing Website", null),
            ("Internal Analytics Dashboard", "Reporting tool for the ops team."),
            // Bob's 4 projects
            ("Customer Support Portal", "Ticketing system for the support team."),
            ("Inventory Management System", "Tracks stock levels across warehouses."),
            ("Personal Blog", null),
            ("DevOps Pipeline Automation", "CI/CD scripts and deployment tooling."),
        };

        var owners = new[] { alice, alice, alice, alice, bob, bob, bob, bob };

        var projects = new List<Project>();
        for (int i = 0; i < projectNames.Length; i++)
        {
            // Project.Create() replaces the old object initializer. It sets
            // CreatedAt/UpdatedAt to UtcNow internally, which isn't what we
            // want for seed data (we want staggered historical dates so
            // created_at sorting has real spread to demonstrate) — so we
            // override them via reflection right after construction.
            var project = Project.Create(projectNames[i].Name, projectNames[i].Description, owners[i].Id);
            var createdAt = now.AddDays(-60 + i * 3);
            SetProperty(project, nameof(Project.CreatedAt), createdAt);
            SetProperty(project, nameof(Project.UpdatedAt), createdAt);
            projects.Add(project);
        }

        context.Set<Project>().AddRange(projects);
        await context.SaveChangesAsync(); // so each project gets its Id before we attach tasks

        foreach (var project in projects)
        {
            context.Set<TaskItem>().AddRange(GenerateTasks(project, now));
        }

        await context.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null) return existing;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to seed user {email}: {errors}");
        }

        return user;
    }

    private static List<TaskItem> GenerateTasks(Project project, DateTime now)
    {
        string[] verbs = { "Implement", "Fix", "Refactor", "Add", "Remove", "Optimize", "Update", "Design", "Test", "Document", "Investigate", "Review" };
        string[] subjects =
        {
            "user authentication", "payment gateway", "search indexing", "email notifications",
            "database schema", "caching layer", "API rate limiting", "file upload",
            "dashboard charts", "logging system", "unit test coverage", "CI pipeline",
            "error handling", "pagination logic", "role-based access"
        };

        Status[] statusCycle = { Status.Todo, Status.InProgress, Status.Done };
        Priority[] priorityCycle = { Priority.Low, Priority.Medium, Priority.High };

        int?[] dueOffsets = { -10, -5, -2, null, 1, 3, 5, 7, 10, 14, 21, 30 };

        var tasks = new List<TaskItem>();
        int seedIndex = project.Id;

        for (int i = 0; i < 12; i++)
        {
            var verb = verbs[i % verbs.Length];
            var subject = subjects[(i + seedIndex) % subjects.Length];
            var createdAt = now.AddDays(-(5 + i * 4));
            var updatedAt = i % 3 == 0 ? createdAt : createdAt.AddHours(i + 1);
            var dueDate = dueOffsets[i].HasValue ? now.Date.AddDays(dueOffsets[i]!.Value) : (DateTime?)null;
            var description = i % 4 == 0 ? null : $"Task related to {subject} in the {project.Name} project.";

            var task = TaskItem.Create(
                $"{verb} {subject}",
                description,
                statusCycle[i % statusCycle.Length],
                priorityCycle[(i + 1) % priorityCycle.Length],
                dueDate,
                project.Id);

            SetProperty(task, nameof(TaskItem.CreatedAt), createdAt);
            SetProperty(task, nameof(TaskItem.UpdatedAt), updatedAt);

            tasks.Add(task);
        }

        return tasks;
    }

    // Project/TaskItem expose only Create()/Update() plus private setters —
    // there's no factory parameter for backdating CreatedAt/UpdatedAt, which
    // seed data needs for realistic sort/filter demonstration. Reflection is
    // the pragmatic way to do that without adding seed-only parameters to the
    // domain model itself. If you'd rather keep reflection out of production
    // code, the alternative is an `internal` factory overload on each entity
    // (e.g. `Project.CreateForSeed(...)`) that accepts explicit timestamps.
    private static void SetProperty<T>(T entity, string propertyName, object value)
    {
        var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"{typeof(T).Name} has no property named '{propertyName}'.");
        prop.SetValue(entity, value);
    }
}