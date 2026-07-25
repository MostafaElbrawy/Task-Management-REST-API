using System.ComponentModel.DataAnnotations;

namespace Task_Management.DTOs
{
    public class RegistrationRequest
    {
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
}
