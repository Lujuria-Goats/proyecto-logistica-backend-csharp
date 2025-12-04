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

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, JwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                PhoneNumber = registerDto.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // Asignar rol basado en lo especificado en el registro
            // Por defecto es "Driver" si no se especifica otro
            string roleToAssign = !string.IsNullOrEmpty(registerDto.Role) ? registerDto.Role : "Driver";
            
            // Validar que el rol sea válido (solo Admin o Driver permitidos)
            if (roleToAssign != "Admin" && roleToAssign != "Driver")
            {
                roleToAssign = "Driver"; // Fallback a Driver si rol inválido
            }

            await _userManager.AddToRoleAsync(user, roleToAssign);

            return Ok(new { message = "Usuario registrado exitosamente.", role = roleToAssign });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null)
            {
                return Unauthorized("Credenciales inválidas.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded)
            {
                return Unauthorized("Credenciales inválidas.");
            }

            var token = await _jwtService.GenerateToken(user);

            return Ok(new { token });
        }
    }
}
