using ApexVision.Backend.Data;
using ApexVision.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApexVision.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("admin-summary")]
        public async Task<IActionResult> GetAdminSummary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int adminId))
                return Unauthorized();

            // 1. Definir rango de tiempo (Hoy)
            var today = DateTime.UtcNow.Date;

            // 2. Orders Stats (Filtrado por Admin)
            var ordersQuery = _context.Orders.Where(o => o.AdminId == adminId);

            var totalOrders = await ordersQuery.CountAsync();
            var ordersToday = await ordersQuery.CountAsync(o => o.CreatedAt >= today);
            var pendingOrders = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Pending);
            var inTransitOrders = await ordersQuery.CountAsync(o => o.Status == OrderStatus.InTransit);
            
            // Completados hoy (Completed o Delivered)
            var completedToday = await ordersQuery.CountAsync(o => 
                (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered) && 
                o.DeliveredAt >= today);

            var canceledOrders = await ordersQuery.CountAsync(o => o.Status == OrderStatus.Cancelled);

            // 3. Driver Stats
            // Conductores vinculados explícitamente a este admin
            var linkedDriversCount = await _context.AdminDrivers
                .CountAsync(ad => ad.AdminId == adminId);

            // Conductores activos hoy (los que han entregado pedidos de este admin hoy)
            var activeDriversToday = await ordersQuery
                .Where(o => (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered) && 
                            o.DeliveredAt >= today && 
                            o.DriverId != null)
                .Select(o => o.DriverId)
                .Distinct()
                .CountAsync();

            // 4. Routes Stats
            // Rutas creadas por este admin (templates) o asignadas por él
            // SavedRoute tiene DriverId (dueño) y AssignedByAdminId (quien asignó)
            // Contamos las que o son del admin (templates) o fueron asignadas por él
            var myRoutesQuery = _context.SavedRoutes
                .Where(r => r.DriverId == adminId || r.AssignedByAdminId == adminId);

            var totalRoutes = await myRoutesQuery.CountAsync();
            var activeRoutes = await myRoutesQuery.CountAsync(r => r.IsActive && r.DriverId != adminId);
            
            // 5. Recent Activity (Últimas 5 entregas + Últimas 5 rutas creadas/asignadas)
            var recentDeliveries = await ordersQuery
                .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered)
                .OrderByDescending(o => o.DeliveredAt)
                .Take(5)
                .Select(o => new
                {
                    Type = "Entrega",
                    Message = $"Pedido a {o.Address} completado",
                    Subtext = o.Driver != null ? o.Driver.FullName : "Sin conductor",
                    Date = (DateTime?)(o.DeliveredAt ?? o.CreatedAt)
                })
                .ToListAsync();

            var recentRoutes = await myRoutesQuery
                .OrderByDescending(r => r.CreatedDate)
                .Take(5)
                .Select(r => new
                {
                    Type = "Ruta",
                    Message = r.AssignedByAdminId != null ? $"Ruta '{r.RouteName}' asignada" : $"Plantilla '{r.RouteName}' creada",
                    Subtext = r.Driver != null ? r.Driver.FullName : "Admin",
                    Date = (DateTime?)r.CreatedDate
                })
                .ToListAsync();

            // NUEVO: Rutas completadas (para que aparezcan en el log como "Finalizada")
            var recentCompletedRoutes = await myRoutesQuery
                .Where(r => r.CompletedDate != null)
                .OrderByDescending(r => r.CompletedDate)
                .Take(5)
                .Select(r => new
                {
                    Type = "Ruta Completada",
                    Message = $"Ruta '{r.RouteName}' finalizada",
                    Subtext = r.Driver != null ? r.Driver.FullName : "Conductor",
                    Date = r.CompletedDate
                })
                .ToListAsync();

            var recentActivity = recentDeliveries
                .Concat(recentRoutes)
                .Concat(recentCompletedRoutes)
                .OrderByDescending(x => x.Date)
                .Take(10)
                .ToList();

            return Ok(new
            {
                stats = new
                {
                    orders = new
                    {
                        total = totalOrders,
                        today = ordersToday,
                        pending = pendingOrders,
                        inTransit = inTransitOrders,
                        completedToday = completedToday,
                        canceled = canceledOrders
                    },
                    drivers = new
                    {
                        totalLinked = linkedDriversCount,
                        activeToday = activeDriversToday
                    },
                    routes = new
                    {
                        total = totalRoutes,
                        active = activeRoutes
                    }
                },
                recentActivity
            });
        }
    }
}
