namespace ApexVision.Backend.Services
{
    public interface IOptimizationService
    {
        Task OptimizeRouteAsync(string driverId);
    }
}

