using Microsoft.AspNetCore.Identity;
using Moq;
using Task_Management.Models;

namespace Task_Management.Tests.TestHelpers
{
    public static class MockUserManagerFactory
    {
        public static Mock<UserManager<ApplicationUser>> Create()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mgr = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            return mgr;
        }
    }
}
