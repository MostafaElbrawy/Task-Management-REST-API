using Microsoft.AspNetCore.Identity;
using Moq;
using Task_Management.Models;

namespace Task_Management.Tests.TestHelpers
{
    public static class MockUserManagerFactory
    {
        // Standard pattern for mocking UserManager<T>: its methods (CreateAsync,
        // FindByEmailAsync, CheckPasswordAsync, GetRolesAsync, etc.) are virtual,
        // so Moq can override them directly without needing real underlying
        // stores/validators/hashers — hence the nulls in the constructor call.
        public static Mock<UserManager<ApplicationUser>> Create()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mgr = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            return mgr;
        }
    }
}
