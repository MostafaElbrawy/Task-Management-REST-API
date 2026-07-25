using Task_Management.DTOs;
namespace Task_Management.Services
{
    public interface IAccountService
    {
        Task<ApiResponse<LoginResponse?>> Login(LoginRequest request);
        Task<ApiResponse<LoginResponse?>> Register(RegistrationRequest request);
    }
}
