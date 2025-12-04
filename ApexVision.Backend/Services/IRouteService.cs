using ApexVision.Backend.DTOs;
using ApexVision.Backend.Models;

namespace ApexVision.Backend.Services
{
    public interface IRouteService
    {
        Task<SavedRoute> SaveRouteAsync(int driverId, SaveRouteDto saveRouteDto);
        Task<List<SavedRoute>> GetSavedRoutesAsync(int driverId);
        Task<SavedRoute> GetSavedRouteAsync(int routeId, int driverId);
        Task<SavedRoute> LoadSavedRouteAsync(int routeId, int driverId);
        Task DeleteSavedRouteAsync(int routeId, int driverId);
        Task<SavedRoute> RenameSavedRouteAsync(int routeId, int driverId, string newName);
    }
}

