using ApexVision.Backend.Data;
using ApexVision.Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace ApexVision.Backend.Services
{
    public class OptimizationService : IOptimizationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _scopeFactory;

        public OptimizationService(IHttpClientFactory httpClientFactory, IServiceScopeFactory scopeFactory)
        {
            _httpClientFactory = httpClientFactory;
            _scopeFactory = scopeFactory;
        }

        public async Task OptimizeRouteAsync(string driverId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var pendingOrders = await context.Orders
                    .Where(o => o.Driver != null && o.Driver.Id.ToString() == driverId && o.Status != OrderStatus.Completed)
                    .OrderBy(o => o.Id) // Consistent ordering
                    .ToListAsync();

                if (!pendingOrders.Any())
                {
                    return; // No orders to optimize
                }

                var payload = pendingOrders.Select(o => new { o.Latitude, o.Longitude }).ToList();
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var httpClient = _httpClientFactory.CreateClient("JavaOptimizationApi");
                var response = await httpClient.PostAsync("api/v1/optimize", content);

                response.EnsureSuccessStatusCode();

                var optimizedRoute = await response.Content.ReadFromJsonAsync<List<OptimizedStop>>();
                if (optimizedRoute == null) return;

                for (int i = 0; i < optimizedRoute.Count; i++)
                {
                    var optimizedStop = optimizedRoute[i];
                    // This assumes the Java service returns coordinates that can be matched back.
                    // A more robust implementation would involve passing and returning order IDs.
                    var orderToUpdate = pendingOrders.FirstOrDefault(o => o.Latitude == optimizedStop.Latitude && o.Longitude == optimizedStop.Longitude);
                    if (orderToUpdate != null)
                    {
                        // orderToUpdate.VisitOrder = i; // Assuming an 'VisitOrder' property exists
                    }
                }

                await context.SaveChangesAsync();
            }
        }

        private class OptimizedStop
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
    }
}
