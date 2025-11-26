using ApexVision.Backend.DTOs.Auth;
using ApexVision.Backend.Models;
using ApexVision.Backend.Services;
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
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FullName = registerDto.FullName
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // Assign role to the user
            // Assuming registerDto.Role is a string like "Admin" or "Driver"
            await _userManager.AddToRoleAsync(user, registerDto.Role.ToString());

            return Ok(new { message = "Usuario registrado exitosamente." });
        }

        [HttpPost("login")]
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
