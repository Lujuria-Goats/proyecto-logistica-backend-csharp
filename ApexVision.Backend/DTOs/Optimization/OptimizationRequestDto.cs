using System.Collections.Generic;

namespace ApexVision.Backend.DTOs.Optimization
{
    public class OptimizationRequestDto
    {
        public string FleetId { get; set; }
        public List<LocationDto> Locations { get; set; }
    }
}

