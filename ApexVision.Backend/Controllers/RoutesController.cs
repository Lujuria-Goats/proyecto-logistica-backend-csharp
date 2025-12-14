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
        [Authorize(Policy = "AdminOrDriver")]
        public async Task<IActionResult> SaveCurrentRoute([FromBody] SaveRouteDto saveRouteDto)
        {
            User? targetUser;

            if (User.IsInRole("Admin"))
            {
                if (saveRouteDto.DriverId.HasValue)
                {
                    // Opción Legacy/Directa: Admin guarda directamente para un conductor
                    targetUser = await _userManager.FindByIdAsync(saveRouteDto.DriverId.Value.ToString());
                    if (targetUser == null)
                        return NotFound($"No se encontró el conductor con ID {saveRouteDto.DriverId}.");
                }
                else
                {
                    // Nueva Opción: Admin guarda para sí mismo (staging)
                    var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    targetUser = await _userManager.FindByIdAsync(adminId);
                }
            }
            else
            {
                // Es Driver
                targetUser = await GetCurrentDriverAsync();
                if (targetUser == null)
                    return Unauthorized("No se pudo identificar al conductor.");
            }

            if (saveRouteDto.OrderIds.Count == 0)
                return BadRequest("Debe incluir al menos un pedido en la ruta.");

            var orders = await _context.Orders
                .Where(o => saveRouteDto.OrderIds.Contains(o.Id)) // Permitir guardar cualquier orden si eres Admin
                .ToListAsync();

            // Si es Driver, validar que las órdenes le pertenezcan
            if (User.IsInRole("Driver"))
            {
                if (orders.Any(o => o.DriverId != targetUser.Id))
                    return BadRequest("Algunos pedidos no pertenecen a este conductor.");
            }

            if (orders.Count != saveRouteDto.OrderIds.Count)
                return BadRequest("Algunos pedidos no existen o no pertenecen a este conductor.");

            var orderIdsJson = JsonSerializer.Serialize(saveRouteDto.OrderIds);

            var savedRoute = new SavedRoute
            {
                DriverId = targetUser.Id,
                RouteName = saveRouteDto.RouteName,
                OrderIds = orderIdsJson,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.SavedRoutes.Add(savedRoute);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Ruta '{RouteName}' guardada para usuario {UserId}", 
                saveRouteDto.RouteName, targetUser.Id);

            return Ok(new 
            { 
                message = "Ruta guardada exitosamente.", 
                routeId = savedRoute.Id,
                routeName = savedRoute.RouteName,
                orderCount = saveRouteDto.OrderIds.Count,
                phoneNumber = targetUser.PhoneNumber
            });
        }

        /// <summary>
        /// [ADMIN] Obtener todas las rutas guardadas de todos los conductores vinculados
        /// </summary>
        [HttpGet("all")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAllRoutes()
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminId) || !int.TryParse(adminId, out var adminIdInt))
                return Unauthorized();

            // Obtener IDs de conductores vinculados al admin
            var linkedDriverIds = await _context.AdminDrivers
                .Where(ad => ad.AdminId == adminIdInt)
                .Select(ad => ad.DriverId)
                .ToListAsync();

            // Incluir también las rutas del propio admin (templates)
            linkedDriverIds.Add(adminIdInt);

            var routesFromDb = await _context.SavedRoutes
                .Where(r => linkedDriverIds.Contains(r.DriverId) && r.IsActive)
                .Include(r => r.Driver)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            var allRoutes = routesFromDb.Select(r => new
            {
                routeId = r.Id,
                routeName = r.RouteName,
                createdDate = r.CreatedDate,
                lastUsedDate = r.LastUsedDate,
                orderIds = JsonSerializer.Deserialize<List<int>>(r.OrderIds) ?? new List<int>(),
                orderCount = (JsonSerializer.Deserialize<List<int>>(r.OrderIds) ?? new List<int>()).Count,
                isTemplate = r.DriverId == adminIdInt,
                driver = new
                {
                    id = r.Driver.Id,
                    fullName = r.Driver.FullName,
                    phoneNumber = r.Driver.PhoneNumber,
                    email = r.Driver.Email
                }
            }).ToList();

            return Ok(new
            {
                totalRoutes = allRoutes.Count,
                templates = allRoutes.Where(r => r.isTemplate).ToList(),
                assignedRoutes = allRoutes.Where(r => !r.isTemplate).ToList()
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

        [HttpPut("saved/{routeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSavedRoute(int routeId, [FromBody] UpdateRouteDto updateDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminId)) return Unauthorized();

            var savedRoute = await _context.SavedRoutes
                .FirstOrDefaultAsync(r => r.Id == routeId && r.DriverId == int.Parse(adminId));

            if (savedRoute == null)
                return NotFound("Ruta no encontrada o no pertenece al administrador.");

            // Validar existencia de órdenes
            var ordersCount = await _context.Orders
                .CountAsync(o => updateDto.OrderIds.Contains(o.Id));
            
            if (ordersCount != updateDto.OrderIds.Count)
                 return BadRequest("Algunos pedidos no existen.");

            savedRoute.RouteName = updateDto.RouteName;
            savedRoute.OrderIds = JsonSerializer.Serialize(updateDto.OrderIds);
            
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ruta actualizada exitosamente." });
        }

        [HttpPost("saved/{routeId}/assign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignSavedRoute(int routeId, [FromBody] AssignRouteDto assignDto)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminId)) return Unauthorized();
            
            // 1. Buscar la ruta original (template) del Admin
            var sourceRoute = await _context.SavedRoutes
                .FirstOrDefaultAsync(r => r.Id == routeId && r.DriverId == int.Parse(adminId));

            if (sourceRoute == null)
                return NotFound("Ruta original no encontrada en tus guardados.");

            // 2. Validar Conductor destino por Teléfono
            var targetDriver = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == assignDto.DriverPhoneNumber);
            if (targetDriver == null)
                return NotFound($"No se encontró ningún conductor con el teléfono {assignDto.DriverPhoneNumber}.");
            
            // 3. Crear copia para el conductor
            var newRoute = new SavedRoute
            {
                DriverId = targetDriver.Id,
                RouteName = sourceRoute.RouteName, // Opcional: Podríamos agregar "(Asignada)"
                OrderIds = sourceRoute.OrderIds,
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                OptimizationScore = sourceRoute.OptimizationScore
            };

            _context.SavedRoutes.Add(newRoute);
            
            // 4. Actualizar DriverId de las órdenes asociadas para que el conductor las vea
            // IMPORTANTE: Al asignar la ruta, ¿debemos mover las órdenes al conductor?
            // Generalmente SÍ, si el objetivo es que él las entregue.
            var orderIds = JsonSerializer.Deserialize<List<int>>(sourceRoute.OrderIds) ?? new();
            var ordersToUpdate = await _context.Orders
                .Where(o => orderIds.Contains(o.Id))
                .ToListAsync();

            foreach(var order in ordersToUpdate)
            {
                order.DriverId = targetDriver.Id;
            }

            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                message = $"Ruta asignada exitosamente al conductor {targetDriver.FullName}.",
                assignedRouteId = newRoute.Id
            });
        }
    }
}
