using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApexVision.Backend.Models;
using Microsoft.AspNetCore.Identity;
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

            _logger.LogInformation("--- Generating JWT Token ---");
            _logger.LogInformation("Jwt:Issuer   = {JwtIssuer}", jwtIssuer);
            _logger.LogInformation("Jwt:Audience = {JwtAudience}", jwtAudience);
            _logger.LogInformation("----------------------------");

            var tokenHandler = new JwtSecurityTokenHandler();
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("La clave JWT no está configurada.");
            }
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("userName", user.UserName ?? string.Empty),
                new Claim("fullName", user.FullName ?? string.Empty),
                new Claim("phoneNumber", user.PhoneNumber ?? string.Empty)
            };

            // Agregar claims de empresa para Admin
            if (!string.IsNullOrEmpty(user.CompanyNit))
            {
                claims.Add(new Claim("companyId", user.Id.ToString())); // El ID del admin es el ID de la empresa
                claims.Add(new Claim("companyNit", user.CompanyNit));
                claims.Add(new Claim("companyName", user.CompanyName ?? string.Empty));
            }

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                // Usar ClaimTypes.Role para que sea el tipo de claim estándar
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            _logger.LogInformation("Roles being added to JWT for user {UserEmail}: {Roles}", user.Email, string.Join(", ", roles));
            _logger.LogInformation("CompanyNit in token: {CompanyNit}", user.CompanyNit ?? "N/A");

            var expirationMinutes = _configuration.GetValue<double>("Jwt:ExpirationMinutes", 60);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = jwtIssuer,
                Audience = jwtAudience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
