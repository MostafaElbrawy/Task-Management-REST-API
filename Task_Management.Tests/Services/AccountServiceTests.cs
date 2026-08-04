using Microsoft.AspNetCore.Identity;
using Moq;
using Task_Management.DTOs;
using Task_Management.Models;
using Task_Management.Services;
using Task_Management.Tests.TestHelpers;
using Xunit;

namespace Task_Management.Tests.Services
{
    public class AccountServiceTests
    {
        private readonly Mock<IJwtService> _jwtService = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly AccountService _sut;

        public AccountServiceTests()
        {
            _userManager = MockUserManagerFactory.Create();
            _sut = new AccountService(_jwtService.Object, _userManager.Object);
        }

        [Fact]
        public async Task Login_NullEmailOrPassword_ReturnsValidationError()
        {
            var result = await _sut.Login(new LoginRequest { Email = null!, Password = null! });

            Assert.False(result.Success);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task Login_UnknownEmail_ReturnsValidationError()
        {
            _userManager.Setup(m => m.FindByEmailAsync("nobody@example.com")).ReturnsAsync((ApplicationUser?)null);

            var result = await _sut.Login(new LoginRequest { Email = "nobody@example.com", Password = "whatever" });

            Assert.False(result.Success);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsValidationError()
        {
            var user = new ApplicationUser { Id = 1, Email = "alice@example.com", UserName = "alice@example.com" };
            _userManager.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);

            var result = await _sut.Login(new LoginRequest { Email = user.Email, Password = "wrong" });

            Assert.False(result.Success);
            Assert.Equal(422, result.StatusCode);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsTokenAndRoles()
        {
            var user = new ApplicationUser { Id = 1, Email = "alice@example.com", UserName = "alice@example.com" };
            _userManager.Setup(m => m.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManager.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
            _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            _jwtService.Setup(j => j.GenerateToken(user)).ReturnsAsync(new JwtDto { AccessToken = "token123", ExpiresIn = 3600 });

            var result = await _sut.Login(new LoginRequest { Email = user.Email, Password = "correct" });

            Assert.True(result.Success);
            Assert.Equal("token123", result.Data!.AccessToken);
            Assert.Contains("User", result.Data.Roles);
        }

        [Fact]
        public async Task Register_CreateFails_ReturnsFailWithErrors()
        {
            _userManager
                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

            var request = new RegistrationRequest
            {
                Email = "new@example.com",
                Password = "weak",
                ConfirmPassword = "weak",
                PhoneNumber = "0100000000"
            };
            var result = await _sut.Register(request);

            Assert.False(result.Success);
            Assert.Contains("Password too weak", result.Errors);
        }

        [Fact]
        public async Task Register_Success_ReturnsCreatedWithToken()
        {
            _userManager
                .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _jwtService
                .Setup(j => j.GenerateToken(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new JwtDto { AccessToken = "newtoken", ExpiresIn = 3600 });

            var request = new RegistrationRequest
            {
                Email = "new@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                PhoneNumber = "0100000000"
            };
            var result = await _sut.Register(request);

            Assert.True(result.Success);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal("new@example.com", result.Data!.Email);
            Assert.Empty(result.Data.Roles);
            Assert.Equal("newtoken", result.Data.AccessToken);
        }
    }
}
