using Task_Management.DTOs;
using Task_Management.Models;

namespace Task_Management.Services
{
    public interface IJwtService
    {
        Task<JwtDto?> GenerateToken(ApplicationUser user);
    }
}
