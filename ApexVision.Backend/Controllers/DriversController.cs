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
    /// Controlador para que el Admin gestione sus conductores vinculados
    /// Los conductores se registran en la app móvil y el Admin los vincula por teléfono
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
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
            // Intentar con ClaimTypes.NameIdentifier primero
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Si no funciona, intentar con "nameid" (claim corto)
            if (string.IsNullOrEmpty(userId))
                userId = User.FindFirst("nameid")?.Value;
            
            // Si aún no funciona, intentar con "sub"
            if (string.IsNullOrEmpty(userId))
                userId = User.FindFirst("sub")?.Value;

            _logger.LogInformation("GetCurrentAdminId - userId extraído: {UserId}, Claims: {Claims}", 
                userId ?? "NULL", 
                string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
            
            return int.TryParse(userId, out var id) ? id : 0;
        }

        /// <summary>
        /// Obtener todos los conductores vinculados al Admin
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyDrivers()
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            var linkedDrivers = await _context.AdminDrivers
                .Where(ad => ad.AdminId == adminId)
                .Include(ad => ad.Driver)
                .ThenInclude(d => d.Orders)
                .Select(ad => new DriverResponseDto
                {
                    Id = ad.Driver.Id,
                    UserName = ad.Driver.UserName ?? "",
                    FullName = ad.Driver.FullName,
                    Email = ad.Driver.Email ?? "",
                    PhoneNumber = ad.Driver.PhoneNumber ?? "",
                    TotalOrders = ad.Driver.Orders.Count(o => o.AdminId == adminId),
                    PendingOrders = ad.Driver.Orders.Count(o => o.AdminId == adminId && o.Status == OrderStatus.Pending),
                    LinkedAt = ad.LinkedAt
                })
                .ToListAsync();

            return Ok(new
            {
                totalDrivers = linkedDrivers.Count,
                drivers = linkedDrivers
            });
        }

        /// <summary>
        /// Vincular un conductor existente por número de teléfono
        /// El conductor debe haberse registrado previamente en la app móvil
        /// </summary>
        [HttpPost("link")]
        public async Task<IActionResult> LinkDriver([FromBody] LinkDriverDto dto)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            // Buscar el conductor por teléfono
            var driver = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber);

            if (driver == null)
                return NotFound(new { message = "No se encontró un conductor con ese número de teléfono. El conductor debe registrarse primero en la app móvil." });

            // Verificar que sea un Driver
            var roles = await _userManager.GetRolesAsync(driver);
            if (!roles.Contains("Driver"))
                return BadRequest(new { message = "El usuario encontrado no es un conductor." });

            // Verificar si ya está vinculado
            var alreadyLinked = await _context.AdminDrivers
                .AnyAsync(ad => ad.AdminId == adminId && ad.DriverId == driver.Id);

            if (alreadyLinked)
                return BadRequest(new { message = "Este conductor ya está vinculado a tu cuenta." });

            // Crear la vinculación
            var adminDriver = new AdminDriver
            {
                AdminId = adminId,
                DriverId = driver.Id,
                LinkedAt = DateTime.UtcNow
            };

            _context.AdminDrivers.Add(adminDriver);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin {AdminId} vinculó al conductor {DriverId} ({Phone})", 
                adminId, driver.Id, dto.PhoneNumber);

            return Ok(new
            {
                message = "Conductor vinculado exitosamente.",
                driver = new DriverResponseDto
                {
                    Id = driver.Id,
                    UserName = driver.UserName ?? "",
                    FullName = driver.FullName,
                    Email = driver.Email ?? "",
                    PhoneNumber = driver.PhoneNumber ?? "",
                    TotalOrders = 0,
                    PendingOrders = 0,
                    LinkedAt = adminDriver.LinkedAt
                }
            });
        }

        /// <summary>
        /// Obtener un conductor específico vinculado
        /// </summary>
        [HttpGet("{driverId}")]
        public async Task<IActionResult> GetDriver(int driverId)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            var linkedDriver = await _context.AdminDrivers
                .Where(ad => ad.AdminId == adminId && ad.DriverId == driverId)
                .Include(ad => ad.Driver)
                .ThenInclude(d => d.Orders)
                .Select(ad => new DriverResponseDto
                {
                    Id = ad.Driver.Id,
                    UserName = ad.Driver.UserName ?? "",
                    FullName = ad.Driver.FullName,
                    Email = ad.Driver.Email ?? "",
                    PhoneNumber = ad.Driver.PhoneNumber ?? "",
                    TotalOrders = ad.Driver.Orders.Count(o => o.AdminId == adminId),
                    PendingOrders = ad.Driver.Orders.Count(o => o.AdminId == adminId && o.Status == OrderStatus.Pending),
                    LinkedAt = ad.LinkedAt
                })
                .FirstOrDefaultAsync();

            if (linkedDriver == null)
                return NotFound(new { message = "Conductor no encontrado o no está vinculado a tu cuenta." });

            return Ok(linkedDriver);
        }

        /// <summary>
        /// Desvincular un conductor (no lo elimina, solo quita la vinculación)
        /// </summary>
        [HttpDelete("{driverId}")]
        public async Task<IActionResult> UnlinkDriver(int driverId)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            var adminDriver = await _context.AdminDrivers
                .FirstOrDefaultAsync(ad => ad.AdminId == adminId && ad.DriverId == driverId);

            if (adminDriver == null)
                return NotFound(new { message = "Conductor no encontrado o no está vinculado a tu cuenta." });

            // Desasignar pedidos pendientes de este conductor con este admin
            var pendingOrders = await _context.Orders
                .Where(o => o.DriverId == driverId && o.AdminId == adminId && o.Status == OrderStatus.Pending)
                .ToListAsync();

            foreach (var order in pendingOrders)
            {
                order.DriverId = null;
            }

            _context.AdminDrivers.Remove(adminDriver);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin {AdminId} desvinculó al conductor {DriverId}. Pedidos desasignados: {Count}", 
                adminId, driverId, pendingOrders.Count);

            return Ok(new { 
                message = "Conductor desvinculado exitosamente.", 
                unassignedOrders = pendingOrders.Count 
            });
        }

        /// <summary>
        /// Buscar conductor por teléfono (para verificar si existe antes de vincular)
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchByPhone([FromQuery] string phone)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest(new { message = "Debe proporcionar un número de teléfono." });

            // Buscar el conductor por teléfono
            var driver = await _context.Users
                .Where(u => u.PhoneNumber != null && u.PhoneNumber.Contains(phone))
                .FirstOrDefaultAsync();

            if (driver == null)
                return NotFound(new { message = "No se encontró un conductor con ese número. Debe registrarse primero en la app móvil." });

            // Verificar que sea un Driver
            var roles = await _userManager.GetRolesAsync(driver);
            if (!roles.Contains("Driver"))
                return NotFound(new { message = "No se encontró un conductor con ese número." });

            // Verificar si ya está vinculado
            var isLinked = await _context.AdminDrivers
                .AnyAsync(ad => ad.AdminId == adminId && ad.DriverId == driver.Id);

            return Ok(new
            {
                found = true,
                alreadyLinked = isLinked,
                driver = new
                {
                    id = driver.Id,
                    fullName = driver.FullName,
                    phoneNumber = driver.PhoneNumber,
                    email = driver.Email
                }
            });
        }
    }

    /// <summary>
    /// DTO para vincular conductor por teléfono
    /// </summary>
    public class LinkDriverDto
    {
        [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
        [RegularExpression(@"^[\d\+\-\(\)\s]{7,}$", ErrorMessage = "Formato de teléfono inválido.")]
        public required string PhoneNumber { get; set; }
    }
}

