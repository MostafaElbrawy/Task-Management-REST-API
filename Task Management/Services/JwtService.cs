using Task_Management.DTOs;
using Microsoft.AspNetCore.Identity;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Task_Management.Models;

namespace Task_Management.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        public JwtService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }
        
        public async Task<JwtDto?> GenerateToken(ApplicationUser user)
        {

            var secretKey = _configuration["JwtConfig:Key"] ;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var issuer = _configuration["JwtConfig:Issuer"];
            var audience = _configuration["JwtConfig:Audience"];
            int tokenValidityInMins = _configuration.GetValue<int>("JwtConfig:TokenValidityMins");
            DateTime expiresIn = DateTime.UtcNow.AddMinutes(tokenValidityInMins);

            List<Claim> claims =
            [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()) 
            ];

            if (!string.IsNullOrEmpty(user.Email))
                claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email!));

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresIn,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var handler = new JsonWebTokenHandler();
            var token = handler.CreateToken(tokenDescriptor);

           var tokenDto = new JwtDto
            {
                AccessToken = token,
                ExpiresIn = tokenValidityInMins * 60,
            };
            return tokenDto;
        
        }
    }
}
