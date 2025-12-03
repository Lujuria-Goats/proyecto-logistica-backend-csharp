using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApexVision.Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ApexVision.Backend.Services
{
    public class JwtService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration configuration, UserManager<User> userManager, ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<string> GenerateToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            _logger.LogInformation("--- Generating JWT Token with following configuration ---");
            _logger.LogInformation("Jwt:Key      = {JwtKey}", jwtKey);
            _logger.LogInformation("Jwt:Issuer   = {JwtIssuer}", jwtIssuer);
            _logger.LogInformation("Jwt:Audience = {JwtAudience}", jwtAudience);
            _logger.LogInformation("----------------------------------------------------");

            var tokenHandler = new JwtSecurityTokenHandler();
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("La clave JWT no está configurada.");
            }
            var key = Encoding.UTF8.GetBytes(jwtKey); // Corregido de ASCII a UTF8

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var expirationMinutes = _configuration.GetValue<double>("Jwt:ExpirationMinutes", 60);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes), // Corregido para usar la configuración
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
