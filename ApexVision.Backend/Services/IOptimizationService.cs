using ApexVision.Backend.Models;

namespace ApexVision.Backend.Services
{
    public interface IOptimizationService
    {
        Task<List<Order>> OptimizeRouteAsync(List<Order> orders);
    }
}