using ApexVision.Backend.Data;
using ApexVision.Backend.DTOs;
using ApexVision.Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ApexVision.Backend.Services
{
    public class RouteService : IRouteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RouteService> _logger;

        public RouteService(ApplicationDbContext context, ILogger<RouteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SavedRoute> SaveRouteAsync(int driverId, SaveRouteDto saveRouteDto)
        {
            var orderIdsJson = JsonSerializer.Serialize(saveRouteDto.OrderIds);

            var savedRoute = new SavedRoute
            {
                DriverId = driverId,
                RouteName = saveRouteDto.RouteName,
                OrderIds = orderIdsJson,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.SavedRoutes.Add(savedRoute);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Ruta '{RouteName}' guardada para conductor {DriverId}", 
                saveRouteDto.RouteName, driverId);

            return savedRoute;
        }

        public async Task<List<SavedRoute>> GetSavedRoutesAsync(int driverId)
        {
            return await _context.SavedRoutes
                .Where(r => r.DriverId == driverId && r.IsActive)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<SavedRoute> GetSavedRouteAsync(int routeId, int driverId)
        {
            return await _context.SavedRoutes
                .FirstOrDefaultAsync(r => r.Id == routeId && r.DriverId == driverId);
        }

        public async Task<SavedRoute> LoadSavedRouteAsync(int routeId, int driverId)
        {
            var route = await _context.SavedRoutes
                .FirstOrDefaultAsync(r => r.Id == routeId && r.DriverId == driverId && r.IsActive);

            if (route != null)
            {
                route.LastUsedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ruta '{RouteName}' cargada para conductor {DriverId}", 
                    route.RouteName, driverId);
            }

            return route;
        }

        public async Task DeleteSavedRouteAsync(int routeId, int driverId)
        {
            var route = await _context.SavedRoutes
                .FirstOrDefaultAsync(r => r.Id == routeId && r.DriverId == driverId);

            if (route != null)
            {
                route.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ruta '{RouteName}' desactivada para conductor {DriverId}", 
                    route.RouteName, driverId);
            }
        }

        public async Task<SavedRoute> RenameSavedRouteAsync(int routeId, int driverId, string newName)
        {
            var route = await _context.SavedRoutes
                .FirstOrDefaultAsync(r => r.Id == routeId && r.DriverId == driverId);

            if (route != null)
            {
                route.RouteName = newName;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ruta renombrada a '{NewName}' para conductor {DriverId}", 
                    newName, driverId);
            }

            return route;
        }
    }
}

