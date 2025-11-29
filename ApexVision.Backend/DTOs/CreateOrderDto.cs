using System.ComponentModel.DataAnnotations;

namespace ApexVision.Backend.DTOs
{
    public class CreateOrderDto
    {
        [Required]
        public string? Description { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        [Required]
        public string? Address { get; set; }

        public bool RequiresEvidence { get; set; }
    }
}
