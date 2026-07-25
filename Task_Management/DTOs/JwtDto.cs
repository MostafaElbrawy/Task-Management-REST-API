namespace Task_Management.DTOs
{
    public class JwtDto
    {
        public required string AccessToken { get; set; }
        public required int ExpiresIn { get; set; }
    }
}
