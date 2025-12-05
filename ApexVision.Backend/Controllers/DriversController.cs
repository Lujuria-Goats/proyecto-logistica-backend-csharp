using ApexVision.Backend.Data;
using ApexVision.Backend.DTOs;
using ApexVision.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ApexVision.Backend.Controllers
{
    /// <summary>
    /// Controlador para que el Admin gestione sus conductores
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class DriversController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<DriversController> _logger;

        public DriversController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<DriversController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        private int GetCurrentAdminId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userId, out var id) ? id : 0;
        }

        /// <summary>
        /// Obtener todos los conductores del Admin
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyDrivers()
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            var drivers = await _context.Users
                .Where(u => u.AdminId == adminId)
                .Select(d => new DriverResponseDto
                {
                    Id = d.Id,
                    UserName = d.UserName ?? "",
                    FullName = d.FullName,
                    Email = d.Email ?? "",
                    PhoneNumber = d.PhoneNumber ?? "",
                    TotalOrders = d.Orders.Count,
                    PendingOrders = d.Orders.Count(o => o.Status == OrderStatus.Pending)
                })
                .ToListAsync();

            return Ok(new
            {
                totalDrivers = drivers.Count,
                drivers = drivers
            });
        }

        /// <summary>
        /// Agregar un nuevo conductor a la empresa
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddDriver([FromBody] AddDriverDto dto)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            // Verificar que el admin existe
            var admin = await _userManager.FindByIdAsync(adminId.ToString());
            if (admin == null)
                return Unauthorized();

            // Verificar username duplicado
            var existingUser = await _userManager.FindByNameAsync(dto.UserName);
            if (existingUser != null)
                return BadRequest(new { message = "El nombre de usuario ya está en uso." });

            // Verificar email duplicado
            existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest(new { message = "El correo electrónico ya está registrado." });

            // Verificar teléfono duplicado
            var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber);
            if (phoneExists)
                return BadRequest(new { message = "El número de teléfono ya está registrado." });

            var driver = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                AdminId = adminId
            };

            var result = await _userManager.CreateAsync(driver, dto.Password);

            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            await _userManager.AddToRoleAsync(driver, "Driver");

            _logger.LogInformation("Admin {AdminId} agregó conductor {DriverId}: {DriverName}", 
                adminId, driver.Id, driver.FullName);

            return Ok(new
            {
                message = "Conductor agregado exitosamente.",
                driver = new DriverResponseDto
                {
                    Id = driver.Id,
                    UserName = driver.UserName ?? "",
                    FullName = driver.FullName,
                    Email = driver.Email ?? "",
                    PhoneNumber = driver.PhoneNumber ?? "",
                    TotalOrders = 0,
                    PendingOrders = 0
                }
            });
        }

        /// <summary>
        /// Obtener un conductor específico
        /// </summary>
        [HttpGet("{driverId}")]
        public async Task<IActionResult> GetDriver(int driverId)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            var driver = await _context.Users
                .Where(u => u.Id == driverId && u.AdminId == adminId)
                .Select(d => new DriverResponseDto
                {
                    Id = d.Id,
                    UserName = d.UserName ?? "",
                    FullName = d.FullName,
                    Email = d.Email ?? "",
                    PhoneNumber = d.PhoneNumber ?? "",
                    TotalOrders = d.Orders.Count,
                    PendingOrders = d.Orders.Count(o => o.Status == OrderStatus.Pending)
                })
                .FirstOrDefaultAsync();

            if (driver == null)
                return NotFound(new { message = "Conductor no encontrado." });

            return Ok(driver);
        }

        /// <summary>
        /// Actualizar datos de un conductor
        /// </summary>
        [HttpPut("{driverId}")]
        public async Task<IActionResult> UpdateDriver(int driverId, [FromBody] UpdateDriverDto dto)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            var driver = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == driverId && u.AdminId == adminId);

            if (driver == null)
                return NotFound(new { message = "Conductor no encontrado." });

            // Actualizar campos si se proporcionan
            if (!string.IsNullOrEmpty(dto.FullName))
                driver.FullName = dto.FullName;

            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != driverId);
                if (phoneExists)
                    return BadRequest(new { message = "El número de teléfono ya está registrado." });
                driver.PhoneNumber = dto.PhoneNumber;
            }

            if (!string.IsNullOrEmpty(dto.Email))
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != driverId);
                if (emailExists)
                    return BadRequest(new { message = "El correo electrónico ya está registrado." });
                driver.Email = dto.Email;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin {AdminId} actualizó conductor {DriverId}", adminId, driverId);

            return Ok(new
            {
                message = "Conductor actualizado exitosamente.",
                driver = new DriverResponseDto
                {
                    Id = driver.Id,
                    UserName = driver.UserName ?? "",
                    FullName = driver.FullName,
                    Email = driver.Email ?? "",
                    PhoneNumber = driver.PhoneNumber ?? ""
                }
            });
        }

        /// <summary>
        /// Eliminar un conductor (soft delete - lo desvincula del admin)
        /// </summary>
        [HttpDelete("{driverId}")]
        public async Task<IActionResult> RemoveDriver(int driverId)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            var driver = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == driverId && u.AdminId == adminId);

            if (driver == null)
                return NotFound(new { message = "Conductor no encontrado." });

            // Verificar si tiene pedidos pendientes
            var hasPendingOrders = await _context.Orders
                .AnyAsync(o => o.DriverId == driverId && o.Status == OrderStatus.Pending);

            if (hasPendingOrders)
                return BadRequest(new { message = "No se puede eliminar un conductor con pedidos pendientes." });

            // Desvincular el conductor del admin (no eliminar el usuario)
            driver.AdminId = null;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin {AdminId} eliminó conductor {DriverId}", adminId, driverId);

            return Ok(new { message = "Conductor eliminado exitosamente." });
        }

        /// <summary>
        /// Buscar conductor por teléfono
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchByPhone([FromQuery] string phone)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest(new { message = "Debe proporcionar un número de teléfono." });

            var driver = await _context.Users
                .Where(u => u.AdminId == adminId && u.PhoneNumber != null && u.PhoneNumber.Contains(phone))
                .Select(d => new DriverResponseDto
                {
                    Id = d.Id,
                    UserName = d.UserName ?? "",
                    FullName = d.FullName,
                    Email = d.Email ?? "",
                    PhoneNumber = d.PhoneNumber ?? "",
                    TotalOrders = d.Orders.Count,
                    PendingOrders = d.Orders.Count(o => o.Status == OrderStatus.Pending)
                })
                .ToListAsync();

            return Ok(new
            {
                results = driver.Count,
                drivers = driver
            });
        }
    }

    /// <summary>
    /// DTO para actualizar conductor
    /// </summary>
    public class UpdateDriverDto
    {
        [MaxLength(100)]
        public string? FullName { get; set; }

        [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        public string? Email { get; set; }

        [RegularExpression(@"^[\d\+\-\(\)\s]{7,}$", ErrorMessage = "Formato de teléfono inválido.")]
        public string? PhoneNumber { get; set; }
    }
}

