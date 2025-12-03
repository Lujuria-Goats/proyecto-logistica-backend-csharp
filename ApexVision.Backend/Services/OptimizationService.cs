using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ApexVision.Backend.Data;
using ApexVision.Backend.DTOs.Optimization;
using Microsoft.EntityFrameworkCore;

namespace ApexVision.Backend.Services
{
    public class OptimizationService : IOptimizationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;

        public OptimizationService(IHttpClientFactory httpClientFactory, ApplicationDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        public async Task OptimizeRouteAsync(string driverId)
        {
            var pendingOrders = await _context.Orders
                .Where(o => o.DriverId.ToString() == driverId && o.Status != Models.OrderStatus.Completed)
                .ToListAsync();

            if (!pendingOrders.Any())
            {
                // No orders to optimize
                return;
            }

            var optimizationRequest = new OptimizationRequestDto
            {
                FleetId = $"driver-{driverId}", // Example fleet ID
                Locations = pendingOrders.Select(o => new LocationDto
                {
                    Id = o.Id,
                    Latitude = o.Latitude,
                    Longitude = o.Longitude,
                    SequenceNumber = null // Java will fill this
                }).ToList()
            };

            var client = _httpClientFactory.CreateClient("JavaOptimizationApi");
            var jsonContent = JsonSerializer.Serialize(optimizationRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/v1/optimize", content);

            response.EnsureSuccessStatusCode();

            // Here you would typically process the response from the Java service
            // to update the VisitOrder of your orders, but for now, we just ensure the call was successful.
        }
    }
}
