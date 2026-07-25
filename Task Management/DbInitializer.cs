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

        // Idempotency guard: if we've already seeded, do nothing.
        // Safe to call this on every app startup.
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
            projects.Add(new Project
            {
                Name = projectNames[i].Name,
                Description = projectNames[i].Description,
                CreatedAt = now.AddDays(-60 + i * 3),
                UpdatedAt = now.AddDays(-60 + i * 3),
                UserId = owners[i].Id
            });
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

            tasks.Add(new TaskItem
            {
                Title = $"{verb} {subject}",
                Description = i % 4 == 0 ? null : $"Task related to {subject} in the {project.Name} project.",
                Status = statusCycle[i % statusCycle.Length],
                Priority = priorityCycle[(i + 1) % priorityCycle.Length],
                DueDate = dueOffsets[i].HasValue ? now.Date.AddDays(dueOffsets[i]!.Value) : null,
                CreatedAt = createdAt,
                UpdatedAt = i % 3 == 0 ? createdAt : createdAt.AddHours(i + 1),
                ProjectId = project.Id
            });
        }

        return tasks;
    }
}