using ApexVision.Backend.DTOs.Auth;
using ApexVision.Backend.Models;
using ApexVision.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ApexVision.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<User> userManager, 
            SignInManager<User> signInManager, 
            JwtService jwtService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <summary>
        /// Registro de Admin (requiere NIT/CC y nombre de empresa)
        /// </summary>
        [HttpPost("register/admin")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminDto dto)
        {
            // Verificar si ya existe el username
            var existingUser = await _userManager.FindByNameAsync(dto.UserName);
            if (existingUser != null)
                return BadRequest(new { message = "El nombre de usuario ya está en uso." });

            // Verificar si ya existe el email
            existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest(new { message = "El correo electrónico ya está registrado." });

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                CompanyNit = dto.CompanyNit,
                CompanyName = dto.CompanyName
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            await _userManager.AddToRoleAsync(user, "Admin");
            
            _logger.LogInformation("Nuevo Admin registrado: {UserName}, Empresa: {CompanyName}", 
                dto.UserName, dto.CompanyName);

            return Ok(new 
            { 
                message = "Admin registrado exitosamente.",
                userId = user.Id,
                userName = user.UserName,
                role = "Admin",
                companyName = user.CompanyName
            });
        }

        /// <summary>
        /// Registro de Driver (chofer)
        /// </summary>
        [HttpPost("register/driver")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterDriver([FromBody] RegisterDriverDto dto)
        {
            // Verificar si ya existe el username
            var existingUser = await _userManager.FindByNameAsync(dto.UserName);
            if (existingUser != null)
                return BadRequest(new { message = "El nombre de usuario ya está en uso." });

            // Verificar si ya existe el email
            existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest(new { message = "El correo electrónico ya está registrado." });

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            await _userManager.AddToRoleAsync(user, "Driver");
            
            _logger.LogInformation("Nuevo Driver registrado: {UserName}", dto.UserName);

            return Ok(new 
            { 
                message = "Driver registrado exitosamente.",
                userId = user.Id,
                userName = user.UserName,
                role = "Driver"
            });
        }


        /// <summary>
        /// Login de usuario (Admin o Driver) - Acepta email, username o número de teléfono
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            User? user = null;

            // 1. Buscar por email
            user = await _userManager.FindByEmailAsync(loginDto.Email);
            
            // 2. Si no encontró por email, buscar por username
            if (user == null)
                user = await _userManager.FindByNameAsync(loginDto.Email);
            
            // 3. Si no encontró por username, buscar por número de teléfono
            if (user == null)
            {
                var users = _userManager.Users.Where(u => u.PhoneNumber == loginDto.Email).ToList();
                user = users.FirstOrDefault();
            }

            if (user == null)
            {
                _logger.LogWarning("Intento de login fallido con identificador: {Identifier}", loginDto.Email);
                return Unauthorized(new { message = "Credenciales inválidas." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Intento de login fallido para usuario: {UserName}", user.UserName);
                return Unauthorized(new { message = "Credenciales inválidas." });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _jwtService.GenerateToken(user);

            _logger.LogInformation("Login exitoso: {UserName}, Rol: {Role}", user.UserName, roles.FirstOrDefault());

            return Ok(new 
            { 
                token,
                userId = user.Id,
                userName = user.UserName,
                fullName = user.FullName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                role = roles.FirstOrDefault() ?? "Driver",
                companyName = user.CompanyName,
                companyNit = user.CompanyNit
            });
        }

        /// <summary>
        /// Obtener información del usuario actual
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                userId = user.Id,
                userName = user.UserName,
                fullName = user.FullName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                role = roles.FirstOrDefault() ?? "Driver",
                companyName = user.CompanyName,
                companyNit = user.CompanyNit
            });
        }
    }
}
