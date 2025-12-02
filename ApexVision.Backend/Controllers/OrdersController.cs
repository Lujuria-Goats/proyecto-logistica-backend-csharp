using ApexVision.Backend.Data;
using ApexVision.Backend.DTOs;
using ApexVision.Backend.Models;
using ApexVision.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Net.Http;

namespace ApexVision.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPhotoService _photoService;
        private readonly IOptimizationService _optimizationService;
        private readonly IAiValidationService _aiValidationService;

        public OrdersController(ApplicationDbContext context, IPhotoService photoService, IOptimizationService optimizationService, IAiValidationService aiValidationService)
        {
            _context = context;
            _photoService = photoService;
            _optimizationService = optimizationService;
            _aiValidationService = aiValidationService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
        {
            if (createOrderDto.Latitude == 0 && createOrderDto.Longitude == 0)
            {
                return BadRequest("Coordinates (0,0) are not allowed.");
            }

            var order = new Order
            {
                Description = createOrderDto.Description ?? "No description",
                Latitude = createOrderDto.Latitude,
                Longitude = createOrderDto.Longitude,
                Address = createOrderDto.Address ?? "No address",
                RequiresEvidence = createOrderDto.RequiresEvidence,
                Status = OrderStatus.Pending,
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order created successfully", orderId = order.Id });
        }

        [HttpPut("{id}/assign/{driverId}")]
        [Authorize(Roles = "Admin")]
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

        [HttpGet]
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Driver")]
        public async Task<IActionResult> GetMyRoute()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var orders = await _context.Orders
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

            return Ok(orders);
        }
        
        [HttpPost("{id}/complete")]
        [Authorize(Roles = "Driver")]
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
                    return BadRequest("Invalid evidence. The image does not seem to be a valid delivery evidence.");
                }
            }

            order.Status = OrderStatus.Completed;
            // In a real app, you'd probably set a completion timestamp here
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order completed successfully." });
        }

        [HttpPost("my-route/optimize")]
        [Authorize(Roles = "Driver")]
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
