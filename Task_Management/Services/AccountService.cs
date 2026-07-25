using Microsoft.AspNetCore.Identity;
using Task_Management.DTOs;
using Task_Management.Models;

namespace Task_Management.Services
{
    public class AccountService : IAccountService
    {
        private readonly IJwtService _jwtService;
        private readonly UserManager<ApplicationUser> _userManager;
        public AccountService(IJwtService jwtService , UserManager<ApplicationUser> userManager) 
        {
            _jwtService = jwtService;
            _userManager = userManager;
        }

        public async Task<ApiResponse<LoginResponse?>> Login(LoginRequest request)
        {
            if (request is null || request.Email is null || request.Password is null)
                return ApiResponse<LoginResponse?>.ValidationError(message: "Invalid Data");

            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user is null)
                return ApiResponse<LoginResponse?>.ValidationError(message: "Invalid email or passowrd");
            

            bool validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!validPassword)
                return ApiResponse<LoginResponse?>.ValidationError(message: "Invalid email or passowrd");

            var userRoles = await _userManager.GetRolesAsync(user);
            var tokenDto = await _jwtService.GenerateToken(user);

            var response = new LoginResponse
            {
                Id = user.Id,
                Email = user.Email!,
                Roles = userRoles.ToList(),
                AccessToken = tokenDto!.AccessToken,
                ExpiresIn = tokenDto!.ExpiresIn,
            };
            return ApiResponse<LoginResponse?>.Ok(response, "Logged in successfully");

        }

        public async Task<ApiResponse<LoginResponse?>> Register(RegistrationRequest request)
        {
            var user = new ApplicationUser()
            {
                Email = request.Email,
                UserName = request.Email,
                PhoneNumber = request.PhoneNumber,
            };
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return ApiResponse<LoginResponse?>
                    .Fail("Error creating an account", result.Errors.Select(e => e.Description).ToList());

            var tokenDto = await _jwtService.GenerateToken(user);

            var response = new LoginResponse
            {
                Id = user.Id,
                Email = user.Email,
                Roles = new List<string>(),
                AccessToken = tokenDto!.AccessToken,
                ExpiresIn = tokenDto!.ExpiresIn,
            };


            return ApiResponse<LoginResponse?>.Created(response, "Account created and logged in successfully");
        }
    }
}
