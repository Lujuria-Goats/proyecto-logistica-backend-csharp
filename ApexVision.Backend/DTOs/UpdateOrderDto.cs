using System.ComponentModel.DataAnnotations;

namespace ApexVision.Backend.DTOs
{
    public class UpdateOrderDto
    {
        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required]
        public string Address { get; set; } = string.Empty;
    }
}
