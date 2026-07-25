using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;
using Task_Management.Controllers;
using Task_Management.Services;
using Task_Management.Tests.TestHelpers;

namespace Task_Management.Tests.Integration
{
    public abstract class IntegrationTestBase : IDisposable
    {
        protected readonly TestDbContext Context;
        protected readonly FakeUnitOfWork UnitOfWork;
        protected readonly ProjectsController ProjectsController;
        protected readonly TasksController TasksController;

        protected IntegrationTestBase()
        {
            Context = TestDbContextFactory.Create();
            UnitOfWork = new FakeUnitOfWork(Context);

            var userManager = MockUserManagerFactory.Create();

            ProjectsController = new ProjectsController(new ProjectService(UnitOfWork, userManager.Object));
            TasksController = new TasksController(new TaskService(UnitOfWork, userManager.Object, NullLogger<TaskService>.Instance));
        }

        // Simulates an authenticated request as the given user id — sets the
        // ClaimTypes.NameIdentifier claim both controllers read to resolve the
        // caller. Bypasses [Authorize]/routing entirely since we call action
        // methods directly rather than going through HTTP.
        protected static void AuthenticateAs(ControllerBase controller, int userId)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        public void Dispose() => Context.Dispose();
    }
}
