using System.ComponentModel.DataAnnotations;

namespace ApexVision.Backend.DTOs
{
    public class AssignRouteDto
    {
        [Required(ErrorMessage = "El número de celular del conductor es obligatorio.")]
        public required string DriverPhoneNumber { get; set; }
    }
}
