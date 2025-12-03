using System.Collections.Generic;

namespace ApexVision.Backend.DTOs.Optimization
{
    public class OptimizationRequestDto
    {
        public string FleetId { get; set; } = string.Empty;
        public List<LocationDto> Locations { get; set; } = new List<LocationDto>();
    }
}
