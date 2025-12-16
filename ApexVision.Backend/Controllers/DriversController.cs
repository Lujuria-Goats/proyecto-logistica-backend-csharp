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
        public async Task<IActionResult> GetMyDrivers([FromQuery] string? query)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            var driversQuery = _context.AdminDrivers
                .Where(ad => ad.AdminId == adminId)
                .Include(ad => ad.Driver)
                .ThenInclude(d => d.Orders)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                driversQuery = driversQuery.Where(ad => 
                    ad.Driver.FullName.ToLower().Contains(query) || 
                    (ad.Driver.PhoneNumber != null && ad.Driver.PhoneNumber.Contains(query)) ||
                    (ad.Driver.Email != null && ad.Driver.Email.ToLower().Contains(query)));
            }

            var linkedDrivers = await driversQuery
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

            // Validar modelo
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
                return Conflict(new { message = "Este conductor ya está vinculado a tu cuenta." });

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

        /// <summary>
        /// Dashboard: Resumen de estadísticas de la empresa
        /// Rutas activas, pedidos pendientes, conductores, etc.
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            // Total de conductores vinculados
            var totalDrivers = await _context.AdminDrivers
                .CountAsync(ad => ad.AdminId == adminId);

            // Conductores activos (con pedidos pendientes o en progreso)
            var activeDrivers = await _context.AdminDrivers
                .Where(ad => ad.AdminId == adminId)
                .Where(ad => ad.Driver.Orders.Any(o => 
                    o.AdminId == adminId && 
                    (o.Status == OrderStatus.Pending || o.Status == OrderStatus.InTransit)))
                .CountAsync();

            // Total de pedidos por estado
            var ordersByStatus = await _context.Orders
                .Where(o => o.AdminId == adminId)
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            var totalOrders = ordersByStatus.Sum(o => o.Count);
            var pendingOrders = ordersByStatus.FirstOrDefault(o => o.Status == "Pending")?.Count ?? 0;
            var inTransitOrders = ordersByStatus.FirstOrDefault(o => o.Status == "InTransit")?.Count ?? 0;
            var deliveredOrders = ordersByStatus.FirstOrDefault(o => o.Status == "Delivered")?.Count ?? 0;

            // Rutas guardadas activas
            var activeRoutes = await _context.SavedRoutes
                .Where(r => r.IsActive)
                .Where(r => _context.AdminDrivers.Any(ad => ad.AdminId == adminId && ad.DriverId == r.DriverId))
                .CountAsync();

            // Entregas de hoy
            var today = DateTime.UtcNow.Date;
            var deliveriesToday = await _context.Orders
                .Where(o => o.AdminId == adminId && o.Status == OrderStatus.Delivered)
                .Where(o => o.DeliveredAt != null && o.DeliveredAt.Value.Date == today)
                .CountAsync();

            return Ok(new
            {
                totalDrivers,
                activeDrivers,
                totalOrders,
                pendingOrders,
                inTransitOrders,
                deliveredOrders,
                activeRoutes,
                deliveriesToday,
                ordersByStatus
            });
        }

        /// <summary>
        /// Obtener actividades recientes de los conductores vinculados
        /// (entregas, asignaciones, rutas completadas, etc.)
        /// </summary>
        [HttpGet("activities")]
        public async Task<IActionResult> GetRecentActivities([FromQuery] int limit = 20)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            // Obtener IDs de conductores vinculados
            var linkedDriverIds = await _context.AdminDrivers
                .Where(ad => ad.AdminId == adminId)
                .Select(ad => ad.DriverId)
                .ToListAsync();

            // Actividades: Pedidos entregados recientemente
            var recentDeliveries = await _context.Orders
                .Where(o => o.AdminId == adminId && o.Status == OrderStatus.Delivered && o.DriverId != null)
                .OrderByDescending(o => o.DeliveredAt)
                .Take(limit)
                .Select(o => new ActivityDto
                {
                    Id = o.Id,
                    Type = "delivery",
                    Description = $"Pedido entregado en {o.Address}",
                    DriverId = o.DriverId,
                    DriverName = o.Driver != null ? o.Driver.FullName : "Desconocido",
                    Timestamp = o.DeliveredAt ?? o.CreatedAt,
                    Details = new { orderId = o.Id, address = o.Address }
                })
                .ToListAsync();

            // Actividades: Pedidos asignados recientemente (creados hoy)
            var recentAssignments = await _context.Orders
                .Where(o => o.AdminId == adminId && o.DriverId != null && o.Status == OrderStatus.Pending)
                .OrderByDescending(o => o.CreatedAt)
                .Take(limit)
                .Select(o => new ActivityDto
                {
                    Id = o.Id,
                    Type = "assignment",
                    Description = $"Pedido asignado: {o.Address}",
                    DriverId = o.DriverId,
                    DriverName = o.Driver != null ? o.Driver.FullName : "Desconocido",
                    Timestamp = o.CreatedAt,
                    Details = new { orderId = o.Id, address = o.Address }
                })
                .ToListAsync();

            // Actividades: Rutas guardadas recientemente
            var recentRoutes = await _context.SavedRoutes
                .Where(r => linkedDriverIds.Contains(r.DriverId) && r.IsActive)
                .OrderByDescending(r => r.CreatedDate)
                .Take(limit)
                .Select(r => new ActivityDto
                {
                    Id = r.Id,
                    Type = "route_saved",
                    Description = $"Ruta guardada: {r.RouteName}",
                    DriverId = r.DriverId,
                    DriverName = r.Driver != null ? r.Driver.FullName : "Desconocido",
                    Timestamp = r.CreatedDate,
                    Details = new { routeId = r.Id, routeName = r.RouteName }
                })
                .ToListAsync();

            // Combinar y ordenar todas las actividades
            var allActivities = recentDeliveries
                .Concat(recentAssignments)
                .Concat(recentRoutes)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToList();

            return Ok(new
            {
                totalActivities = allActivities.Count,
                activities = allActivities
            });
        }

        /// <summary>
        /// Obtener estadísticas detalladas de un conductor específico
        /// </summary>
        [HttpGet("{driverId}/stats")]
        public async Task<IActionResult> GetDriverStats(int driverId)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0)
                return Unauthorized();

            // Verificar que el conductor esté vinculado
            var isLinked = await _context.AdminDrivers
                .AnyAsync(ad => ad.AdminId == adminId && ad.DriverId == driverId);

            if (!isLinked)
                return NotFound(new { message = "Conductor no encontrado o no está vinculado a tu cuenta." });

            var driver = await _context.Users.FindAsync(driverId);
            if (driver == null)
                return NotFound(new { message = "Conductor no encontrado." });

            // Estadísticas del conductor
            var totalOrders = await _context.Orders
                .CountAsync(o => o.DriverId == driverId && o.AdminId == adminId);

            var deliveredOrders = await _context.Orders
                .CountAsync(o => o.DriverId == driverId && o.AdminId == adminId && o.Status == OrderStatus.Delivered);

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.DriverId == driverId && o.AdminId == adminId && o.Status == OrderStatus.Pending);

            var inTransitOrders = await _context.Orders
                .CountAsync(o => o.DriverId == driverId && o.AdminId == adminId && o.Status == OrderStatus.InTransit);

            var savedRoutes = await _context.SavedRoutes
                .CountAsync(r => r.DriverId == driverId && r.IsActive);

            // Entregas de la última semana
            var lastWeek = DateTime.UtcNow.AddDays(-7);
            var deliveriesLastWeek = await _context.Orders
                .CountAsync(o => o.DriverId == driverId && o.AdminId == adminId && 
                    o.Status == OrderStatus.Delivered && o.DeliveredAt >= lastWeek);

            // Entregas de hoy
            var today = DateTime.UtcNow.Date;
            var deliveriesToday = await _context.Orders
                .CountAsync(o => o.DriverId == driverId && o.AdminId == adminId && 
                    o.Status == OrderStatus.Delivered && o.DeliveredAt != null && o.DeliveredAt.Value.Date == today);

            return Ok(new
            {
                driver = new
                {
                    id = driver.Id,
                    fullName = driver.FullName,
                    userName = driver.UserName,
                    phoneNumber = driver.PhoneNumber,
                    email = driver.Email
                },
                stats = new
                {
                    totalOrders,
                    deliveredOrders,
                    pendingOrders,
                    inTransitOrders,
                    savedRoutes,
                    deliveriesLastWeek,
                    deliveriesToday,
                    deliveryRate = totalOrders > 0 ? Math.Round((double)deliveredOrders / totalOrders * 100, 2) : 0
                }
            });
        }

        /// <summary>
        /// Obtener rutas asignadas a un conductor específico
        /// </summary>
        [HttpGet("{driverId}/routes")]
        public async Task<IActionResult> GetDriverRoutes(int driverId)
        {
            var adminId = GetCurrentAdminId();
            if (adminId == 0) return Unauthorized();

            // Verificar si el conductor está vinculado a este admin
            var isLinked = await _context.AdminDrivers
                .AnyAsync(ad => ad.AdminId == adminId && ad.DriverId == driverId);

            if (!isLinked)
                return NotFound("El conductor no está vinculado a tu cuenta.");

            var driverName = await _context.Users
                .Where(u => u.Id == driverId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();

            var routes = await _context.SavedRoutes
                .Where(r => r.DriverId == driverId && r.IsActive)
                .Include(r => r.AssignedByAdmin)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            // Obtener todos los IDs de órdenes de estas rutas para consultarlas en una sola query
            var allOrderIds = routes.SelectMany(r => System.Text.Json.JsonSerializer.Deserialize<List<int>>(r.OrderIds) ?? new List<int>()).Distinct().ToList();
            
            var ordersStatus = await _context.Orders
                .Where(o => allOrderIds.Contains(o.Id))
                .Select(o => new { o.Id, o.Status })
                .ToDictionaryAsync(o => o.Id, o => o.Status);

            var result = routes.Select(r => {
                var orderIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(r.OrderIds) ?? new();
                var total = orderIds.Count;
                var completed = orderIds.Count(id => ordersStatus.ContainsKey(id) && ordersStatus[id] == OrderStatus.Completed);
                
                string statusString;
                if (total == 0) statusString = "Empty";
                else if (completed == 0) statusString = "Pending";
                else if (completed == total) statusString = "Completed";
                else statusString = "InProgress";

                return new
                {
                    id = r.Id,
                    routeName = r.RouteName,
                    orderIds = orderIds,
                    orderCount = total,
                    completedCount = completed,
                    progress = $"{completed}/{total}",
                    status = statusString,
                    createdDate = r.CreatedDate,
                    lastUsedDate = r.LastUsedDate,
                    isActive = r.IsActive,
                    optimizationScore = r.OptimizationScore,
                    assignedBy = r.AssignedByAdmin != null ? new
                    {
                        id = r.AssignedByAdmin.Id,
                        fullName = r.AssignedByAdmin.FullName
                    } : null
                };
            }).ToList();

            return Ok(new
            {
                driverId,
                driverName,
                totalRoutes = result.Count,
                routes = result
            });
        }
    }

    /// <summary>
    /// DTO para actividades recientes
    /// </summary>
    public class ActivityDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public int? DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public object? Details { get; set; }
    }
}
