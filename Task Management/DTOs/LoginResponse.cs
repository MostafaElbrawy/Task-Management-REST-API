namespace Task_Management.DTOs
{
    public class LoginResponse
    {
        public required int Id { get; set; }
        public required string Email { get; set; }
        public required List<string> Roles {  get; set; } 
        public required string AccessToken { get; set; }
        public required int ExpiresIn { get; set; }
    }
}
