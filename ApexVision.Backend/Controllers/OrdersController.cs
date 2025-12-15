using ApexVision.Backend.Data;
using ApexVision.Backend.DTOs;
using ApexVision.Backend.Models;
using ApexVision.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ApexVision.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPhotoService _photoService;
        private readonly IOptimizationService _optimizationService;
        private readonly IAiValidationService _aiValidationService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(ApplicationDbContext context, IPhotoService photoService, IOptimizationService optimizationService, IAiValidationService aiValidationService, ILogger<OrdersController> logger)
        {
            _context = context;
            _photoService = photoService;
            _optimizationService = optimizationService;
            _aiValidationService = aiValidationService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            if (createOrderDto.Latitude == 0 && createOrderDto.Longitude == 0)
            {
                return BadRequest("Coordinates (0,0) are not allowed.");
            }

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (adminId == 0)
                return Unauthorized();

            var order = new Order
            {
                Description = createOrderDto.Description ?? "No description",
                Latitude = createOrderDto.Latitude,
                Longitude = createOrderDto.Longitude,
                Address = createOrderDto.Address ?? "No address",
                RequiresEvidence = createOrderDto.RequiresEvidence,
                Status = OrderStatus.Pending,
                AdminId = adminId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order created successfully", orderId = order.Id });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto updateOrderDto)
        {
            if (updateOrderDto.Latitude == 0 && updateOrderDto.Longitude == 0)
            {
                return BadRequest("Coordinates (0,0) are not allowed.");
            }

            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (adminId == 0)
                return Unauthorized();

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound("Order not found.");
            }

            // Verify order belongs to admin? 
            // The current requirement just says AdminOnly, but typically admins should manage their own orders.
            // However, existing endpoints like 'GetAllOrders' don't filter by AdminId strictly in the query (wait, checking GetAllOrders...).
            // GetAllOrders in line 98 DOES NOT filter by AdminId! It returns ALL orders.
            // But CreateOrder sets AdminId.
            // Let's implement a check if we want strict ownership, but for now I'll follow the pattern. 
            // Actually, let's enforce AdminId check if the order has one, just to be safe, OR minimal implementation first.
            // Given the user prompt didn't specify strict isolation, but previous context did ("filtered per admin"), 
            // I should probably ensure the admin can only update their own orders OR simply update it if they are an admin.
            // Let's look at `DriversController` - it uses `GetCurrentAdminId` and filters.
            // Let's refine this to be safe: check if order.AdminId == adminId.

            if (order.AdminId != null && order.AdminId != adminId)
            {
                return Unauthorized("You do not have permission to edit this order.");
            }

            order.Description = updateOrderDto.Description;
            order.Latitude = updateOrderDto.Latitude;
            order.Longitude = updateOrderDto.Longitude;
            order.Address = updateOrderDto.Address;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order updated successfully." });
        }

        [HttpPut("{id}/assign/{driverId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> AssignDriver(int id, int driverId)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound("Order not found.");
            }

            order.DriverId = driverId;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Driver assigned successfully." });
        }

        [HttpPut("{id}/unassign-driver")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UnassignDriver(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound("Order not found.");
            }
            if (order.DriverId == null)
            {
                return BadRequest("Order is not assigned to any driver.");
            }
            order.DriverId = null;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Driver unassigned successfully." });
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Orders.Select(o => new OrderDto
            {
                Id = o.Id,
                Description = o.Description,
                Latitude = o.Latitude,
                Longitude = o.Longitude,
                Address = o.Address,
                Status = o.Status,
                RequiresEvidence = o.RequiresEvidence,
                DriverId = o.DriverId,
                EvidenceUrl = o.EvidenceUrl
            }).ToListAsync();

            return Ok(orders);
        }

        [HttpGet("my-route")]
        [Authorize(Policy = "DriverOnly")]
        public virtual async Task<IActionResult> GetMyRoute()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var orders = await GetDriverOrdersAsync(userId);
            return Ok(orders);
        }

        // NUEVO: Historial de entregas (todo lo completado)
        [HttpGet("history")]
        [Authorize(Policy = "DriverOnly")]
        public async Task<IActionResult> GetOrderHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Retornar solo las completadas. Podríamos añadir Paginación aquí (Take/Skip).
            var history = await _context.Orders
                .Where(o => o.Driver != null && o.Driver.Id.ToString() == userId && o.Status == OrderStatus.Completed)
                .OrderByDescending(o => o.DeliveredAt)
                .ThenByDescending(o => o.Id) // Ordenar por fecha entrega o ID
                .Take(50) // Limite de seguridad para no explotar el payload
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    Description = o.Description,
                    Address = o.Address,
                    Status = o.Status,
                    EvidenceUrl = o.EvidenceUrl,
                    DeliveredAt = o.DeliveredAt // Asegurarse de tener esta propiedad en OrderDto si es necesario, o usar metadatos
                })
                .ToListAsync();

            return Ok(history);
        }

        // NUEVO: Resumen de Ruta (Estadísticas para barra de progreso)
        [HttpGet("route-summary")]
        [Authorize(Policy = "DriverOnly")]
        public async Task<IActionResult> GetRouteSummary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var stats = await _context.Orders
                .Where(o => o.Driver != null && o.Driver.Id.ToString() == userId)
                .GroupBy(o => 1) // Grupo dummy para agregar todo
                .Select(g => new
                {
                    Total = g.Count(),
                    Pending = g.Count(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.InTransit),
                    Completed = g.Count(o => o.Status == OrderStatus.Completed)
                })
                .FirstOrDefaultAsync();

            return Ok(stats ?? new { Total = 0, Pending = 0, Completed = 0 });
        }

        // Método protegido virtual para facilitar las pruebas
        protected virtual async Task<List<OrderDto>> GetDriverOrdersAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.Driver)  // Asegurarse de cargar la relación Driver
                .Where(o => o.Driver != null && o.Driver.Id.ToString() == userId && o.Status != OrderStatus.Completed)
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    Description = o.Description,
                    Latitude = o.Latitude,
                    Longitude = o.Longitude,
                    Address = o.Address,
                    Status = o.Status,
                    RequiresEvidence = o.RequiresEvidence,
                    DriverId = o.DriverId,
                    EvidenceUrl = o.EvidenceUrl
                })
                .ToListAsync();
        }
        
        [HttpPost("{id}/complete")]
        [Authorize(Policy = "DriverOnly")]
        public async Task<IActionResult> CompleteOrder(int id, IFormFile? file)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound("Order not found.");
            }

            if (order.RequiresEvidence)
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Evidence file is required for this order.");
                }

                var uploadResult = await _photoService.AddPhotoAsync(file);
                if (uploadResult.Error != null)
                {
                    return BadRequest(uploadResult.Error.Message);
                }
                order.EvidenceUrl = uploadResult.SecureUrl.AbsoluteUri;

                var isValid = await _aiValidationService.ValidateDeliveryEvidenceAsync(order.EvidenceUrl);
                if (!isValid)
                {
                    return BadRequest("La foto no parece mostrar un paquete o entrega. Por favor, toma una foto clara del paquete.");
                }
            }

            order.Status = OrderStatus.Completed;
            // In a real app, you\'d probably set a completion timestamp here
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order completed successfully." });
        }

        [HttpPost("my-route/optimize")]
        [Authorize(Policy = "DriverOnly")]
        public async Task<IActionResult> OptimizeMyRoute()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                await _optimizationService.OptimizeRouteAsync(userId);
                return Ok(new { message = "Route optimization initiated." });
            }
            catch (HttpRequestException e)
            {
                // Log the error
                return StatusCode(503, new { message = "The optimization service is currently unavailable.", details = e.Message });
            }
            catch (Exception e)
            {
                // Log the error
                return StatusCode(500, new { message = "An unexpected error occurred.", details = e.Message });
            }
        }
    }
}
