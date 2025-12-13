using ApexVision.Backend.Data;
using ApexVision.Backend.DTOs;
using ApexVision.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace ApexVision.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoutesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<RoutesController> _logger;

        public RoutesController(ApplicationDbContext context, UserManager<User> userManager, ILogger<RoutesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        private async Task<User?> GetCurrentDriverAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out _))
                return null;
            return await _userManager.FindByIdAsync(userId);
        }

        [HttpPost("save")]
        [Authorize(Roles = "Admin,Driver")]
        public async Task<IActionResult> SaveCurrentRoute([FromBody] SaveRouteDto saveRouteDto)
        {
            User? driver;

            if (User.IsInRole("Admin"))
            {
                if (saveRouteDto.DriverId == null)
                    return BadRequest("El administrador debe especificar el ID del conductor (DriverId).");

                driver = await _userManager.FindByIdAsync(saveRouteDto.DriverId.Value.ToString());
                if (driver == null)
                    return NotFound($"No se encontró el conductor con ID {saveRouteDto.DriverId}.");

                // Opcional: Verificar que el usuario destino sea realmente un Driver
                if (!await _userManager.IsInRoleAsync(driver, "Driver"))
                    return BadRequest("El usuario especificado no tiene el rol de conductor.");
            }
            else
            {
                driver = await GetCurrentDriverAsync();
                if (driver == null)
                    return Unauthorized("No se pudo identificar al conductor.");
            }

            if (saveRouteDto.OrderIds.Count == 0)
                return BadRequest("Debe incluir al menos un pedido en la ruta.");

            var orders = await _context.Orders
                .Where(o => saveRouteDto.OrderIds.Contains(o.Id) && o.DriverId == driver.Id)
                .ToListAsync();

            if (orders.Count != saveRouteDto.OrderIds.Count)
                return BadRequest("Algunos pedidos no existen o no pertenecen a este conductor.");

            var orderIdsJson = JsonSerializer.Serialize(saveRouteDto.OrderIds);

            var savedRoute = new SavedRoute
            {
                DriverId = driver.Id,
                RouteName = saveRouteDto.RouteName,
                OrderIds = orderIdsJson,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.SavedRoutes.Add(savedRoute);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Ruta '{RouteName}' guardada para conductor {DriverId}", 
                saveRouteDto.RouteName, driver.Id);

            return Ok(new 
            { 
                message = "Ruta guardada exitosamente.", 
                routeId = savedRoute.Id,
                routeName = savedRoute.RouteName,
                orderCount = saveRouteDto.OrderIds.Count,
                phoneNumber = driver.PhoneNumber
            });
        }

        [HttpGet("saved")]
        [Authorize(Policy = "DriverOnly")]
        public async Task<IActionResult> GetSavedRoutes()
        {
            var driver = await GetCurrentDriverAsync();
            if (driver == null)
                return Unauthorized("No se pudo identificar al conductor.");

            var savedRoutes = await _context.SavedRoutes
                .Where(r => r.DriverId == driver.Id && r.IsActive)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            var result = savedRoutes.Select(r => new SavedRouteDto
            {
                Id = r.Id,
                RouteName = r.RouteName,
                OrderIds = JsonSerializer.Deserialize<List<int>>(r.OrderIds) ?? new(),
                CreatedDate = r.CreatedDate,
                LastUsedDate = r.LastUsedDate,
                IsActive = r.IsActive,
                OptimizationScore = r.OptimizationScore
            }).ToList();

            return Ok(new 
            { 
                phoneNumber = driver.PhoneNumber,
                totalSavedRoutes = result.Count,
                routes = result
            });
        }

        [HttpGet("saved/{routeId}")]
        [Authorize(Policy = "DriverOnly")]
        public async Task<IActionResult> GetSavedRoute(int routeId)
        {
            var driver = await GetCurrentDriverAsync();
            if (driver == null)
                return Unauthorized("No se pudo identificar al conductor.");

            var savedRoute = await _context.SavedRoutes
                .FirstOrDefaultAsync(r => r.Id == routeId && r.DriverId == driver.Id);

            if (savedRoute == null)
                return NotFound("Ruta guardada no encontrada.");

            var orderIds = JsonSerializer.Deserialize<List<int>>(savedRoute.OrderIds) ?? new();
            var orders = await _context.Orders
                .Where(o => orderIds.Contains(o.Id))
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    Address = o.Address,
                    Description = o.Description,
                    Latitude = o.Latitude,
                    Longitude = o.Longitude,
                    Status = o.Status,
                    RequiresEvidence = o.RequiresEvidence,
                    DriverId = o.DriverId,
                    EvidenceUrl = o.EvidenceUrl
                })
                .ToListAsync();

            return Ok(new 
            { 
                routeId = savedRoute.Id,
                routeName = savedRoute.RouteName,
                createdDate = savedRoute.CreatedDate,
                lastUsedDate = savedRoute.LastUsedDate,
                phoneNumber = driver.PhoneNumber,
                orders,
                optimizationScore = savedRoute.OptimizationScore
            });
        }

        [HttpPost("saved/{routeId}/load")]
        [Authorize(Policy = "DriverOnly")]
        public async Task<IActionResult> LoadSavedRoute(int routeId)
        {
            var driver = await GetCurrentDriverAsync();
            if (driver == null)
                return Unauthorized("No se pudo identificar al conductor.");

            var savedRoute = await _context.SavedRoutes
                .FirstOrDefaultAsync(r => r.Id == routeId && r.DriverId == driver.Id && r.IsActive);

            if (savedRoute == null)
                return NotFound("Ruta guardada no encontrada.");

            savedRoute.LastUsedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var orderIds = JsonSerializer.Deserialize<List<int>>(savedRoute.OrderIds) ?? new();
            var orders = await _context.Orders
                .Where(o => orderIds.Contains(o.Id))
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    Address = o.Address,
                    Description = o.Description,
                    Latitude = o.Latitude,
                    Longitude = o.Longitude,
                    Status = o.Status,
                    RequiresEvidence = o.RequiresEvidence,
                    DriverId = o.DriverId,
                    EvidenceUrl = o.EvidenceUrl
                })
                .ToListAsync();

            _logger.LogInformation("Ruta '{RouteName}' cargada para conductor {DriverId}", 
                savedRoute.RouteName, driver.Id);

            return Ok(new 
            { 
                message = "Ruta cargada exitosamente.",
                routeName = savedRoute.RouteName,
                phoneNumber = driver.PhoneNumber,
                totalOrders = orders.Count,
                orders
            });
        }

        [HttpDelete("saved/{routeId}")]
        [Authorize(Policy = "DriverOnly")]
        public async Task<IActionResult> DeleteSavedRoute(int routeId)
        {
            var driver = await GetCurrentDriverAsync();
            if (driver == null)
                return Unauthorized("No se pudo identificar al conductor.");

            var savedRoute = await _context.SavedRoutes
                .FirstOrDefaultAsync(r => r.Id == routeId && r.DriverId == driver.Id);

            if (savedRoute == null)
                return NotFound("Ruta guardada no encontrada.");

            savedRoute.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ruta eliminada exitosamente." });
        }

        [HttpPost("saved/{routeId}/rename")]
        [Authorize(Policy = "DriverOnly")]
        public async Task<IActionResult> RenameSavedRoute(int routeId, [FromBody] RenameRouteDto request)
        {
            var driver = await GetCurrentDriverAsync();
            if (driver == null)
                return Unauthorized("No se pudo identificar al conductor.");

            var savedRoute = await _context.SavedRoutes
                .FirstOrDefaultAsync(r => r.Id == routeId && r.DriverId == driver.Id);

            if (savedRoute == null)
                return NotFound("Ruta guardada no encontrada.");

            if (string.IsNullOrWhiteSpace(request.NewName) || request.NewName.Length > 100)
                return BadRequest("El nombre debe tener entre 1 y 100 caracteres.");

            savedRoute.RouteName = request.NewName;
            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                message = "Ruta renombrada exitosamente.",
                routeId = savedRoute.Id,
                newName = savedRoute.RouteName,
                phoneNumber = driver.PhoneNumber
            });
        }
    }
}
